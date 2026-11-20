namespace LogiMarket360.Application.Facade;

using System.Text.Json;
using LogiMarket360.Application.DTOs;
using LogiMarket360.Application.Patterns.ChainOfResponsibility;
using LogiMarket360.Application.Patterns.Observer;
using LogiMarket360.Application.Patterns.Observer.Eventos;
using LogiMarket360.Application.Patterns.Singleton;
using LogiMarket360.Application.Ports;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Domain.Entities;
using LogiMarket360.Domain.Enums;
using LogiMarket360.Domain.Exceptions;

public record ResultadoConfirmacion(
    bool Exitoso,
    string Mensaje,
    string? ReferenciaPago = null,
    decimal? TotalCobrado = null,
    bool EsIdempotente = false
);

// FACADE: orquesta la confirmación completa de un pedido delegando en sus colaboradores
public class ConfirmacionPedidoFacade
{
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IProductoRepository _productoRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IMovimientoInventarioRepository _movimientoRepository;
    private readonly IPagoRepository _pagoRepository;
    private readonly IPasarelaPago _pasarela;        // Proxy → Decorator → Adapter
    private readonly IEventoOutboxRepository _outboxRepository;
    private readonly IAuditoriaRepository _auditoriaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventoPublicador _eventoPublicador;
    private readonly MetricasSesion _metricas;

    // Chain of Responsibility para validación previa
    private readonly IValidationStep _cadenaValidacion;

    public ConfirmacionPedidoFacade(
        IPedidoRepository pedidoRepository,
        IProductoRepository productoRepository,
        IClienteRepository clienteRepository,
        IMovimientoInventarioRepository movimientoRepository,
        IPagoRepository pagoRepository,
        IPasarelaPago pasarela,
        IEventoOutboxRepository outboxRepository,
        IAuditoriaRepository auditoriaRepository,
        IUnitOfWork unitOfWork,
        IEventoPublicador eventoPublicador,
        MetricasSesion metricas)
    {
        _pedidoRepository = pedidoRepository;
        _productoRepository = productoRepository;
        _clienteRepository = clienteRepository;
        _movimientoRepository = movimientoRepository;
        _pagoRepository = pagoRepository;
        _pasarela = pasarela;
        _outboxRepository = outboxRepository;
        _auditoriaRepository = auditoriaRepository;
        _unitOfWork = unitOfWork;
        _eventoPublicador = eventoPublicador;
        _metricas = metricas;

        // Construir la cadena de validación
        var paso1 = new ClienteActivoValidationStep(clienteRepository);
        var paso2 = new LineasYCantidadesValidationStep();
        var paso3 = new StockDisponibleValidationStep(productoRepository);
        var paso4 = new CondicionConfirmacionValidationStep();
        paso1.SetNext(paso2).SetNext(paso3).SetNext(paso4);
        _cadenaValidacion = paso1;
    }

    // Operación única de confirmación (Facade no contiene SQL ni reglas de descuento)
    public async Task<ResultadoConfirmacion> ConfirmarAsync(
        int pedidoId,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        // 1. Idempotencia: si ya existe pago aprobado con la misma clave, retornar resultado original
        var pedidoExistente = await _pedidoRepository.ObtenerPorIdempotencyKeyAsync(idempotencyKey, ct);
        if (pedidoExistente != null && pedidoExistente.EstadoCodigo == EstadoPedido.PAGADO)
        {
            return new ResultadoConfirmacion(
                Exitoso: true,
                Mensaje: $"El pedido {pedidoExistente.NumeroPedido} ya fue confirmado (idempotente).",
                EsIdempotente: true
            );
        }

        // 2. Cargar el pedido
        var pedido = await _pedidoRepository.ObtenerPorIdAsync(pedidoId, ct);
        if (pedido == null)
        {
            return new ResultadoConfirmacion(false, $"Pedido con ID '{pedidoId}' no encontrado.");
        }

        // 3. Ejecutar cadena de validación (Chain of Responsibility)
        var validacion = await _cadenaValidacion.ValidateAsync(pedido, ct);
        if (!validacion.EsValido)
        {
            return new ResultadoConfirmacion(false, $"[{validacion.PasoFallido}] {validacion.MensajeError}");
        }

        // 4. Validar estado → Validado (si aún está en BORRADOR)
        if (pedido.EstadoCodigo == EstadoPedido.BORRADOR)
        {
            pedido.Validar();
        }

        // 5. Ejecutar cobro simulado (Proxy → Decorator → Adapter)
        ResultadoPago resultadoPago;
        try
        {
            resultadoPago = await _pasarela.CobrarAsync(pedido.Total, idempotencyKey, ct);
        }
        catch (UnauthorizedAccessException uex)
        {
            return new ResultadoConfirmacion(false, $"Acceso denegado: {uex.Message}");
        }

        if (!resultadoPago.Exitoso)
        {
            _metricas.RegistrarCobroFallido();
            return new ResultadoConfirmacion(false, $"Cobro rechazado: {resultadoPago.Mensaje}");
        }

        // 6. Iniciar transacción atómica: stock + pedido + pago + Outbox
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            // 6a. Descontar stock por SKU físico
            await DescontarStockAsync(pedido, ct);

            // 6b. Cambiar estado del pedido a PAGADO
            pedido.Pagar();
            _pedidoRepository.Actualizar(pedido);

            // 6c. Registrar el pago
            var pago = new Pago(
                pedidoId: pedido.Id,
                monto: pedido.Total,
                referenciaTransaccion: resultadoPago.ReferenciaTransaccion,
                claveIdempotencia: idempotencyKey,
                metodoPago: "SIMULADO",
                proveedorSimulado: resultadoPago.Proveedor,
                aprobado: true
            );
            await _pagoRepository.AgregarAsync(pago, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            // 6d. Crear evento Outbox en la misma transacción
            var eventoPayload = JsonSerializer.Serialize(new
            {
                pedidoId = pedido.Id,
                numeroPedido = pedido.NumeroPedido,
                total = pedido.Total,
                referencia = resultadoPago.ReferenciaTransaccion,
                fecha = DateTime.UtcNow
            });
            var outbox = new EventoOutbox("PedidoPagado", eventoPayload);
            await _outboxRepository.AgregarAsync(outbox, ct);

            // 6e. Auditoría en la misma transacción
            var audit = new Auditoria(
                usuario: "SISTEMA",
                operacion: "CONFIRMACION_PEDIDO",
                entidad: "Pedido",
                entidadId: pedido.Id.ToString(),
                detalles: $"Total USD {pedido.Total:F2} | Ref: {resultadoPago.ReferenciaTransaccion}",
                exitoso: true
            );
            await _auditoriaRepository.AgregarAsync(audit, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitAsync(ct);

            // 7. Post-transacción: publicar evento (Observer)
            await _eventoPublicador.PublicarAsync(new PedidoPagadoEvent(
                pedido.Id,
                pedido.NumeroPedido,
                pedido.ClienteId,
                pedido.Total,
                resultadoPago.ReferenciaTransaccion,
                DateTime.UtcNow
            ), ct);

            return new ResultadoConfirmacion(
                Exitoso: true,
                Mensaje: $"Pedido {pedido.NumeroPedido} confirmado y cobrado exitosamente.",
                ReferenciaPago: resultadoPago.ReferenciaTransaccion,
                TotalCobrado: pedido.Total
            );
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync(ct);
            return new ResultadoConfirmacion(false, $"Error al confirmar: {ex.Message}");
        }
    }

    private async Task DescontarStockAsync(Pedido pedido, CancellationToken ct)
    {
        // Consolidar requerimientos por SKU
        var requerimientos = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var detalle in pedido.Detalles)
        {
            if (detalle.ComponentesSnapshot.Count > 0)
            {
                foreach (var comp in detalle.ComponentesSnapshot)
                    requerimientos[comp.Sku] = requerimientos.GetValueOrDefault(comp.Sku) + comp.CantidadFisica;
            }
            else
            {
                requerimientos[detalle.CodigoItem] = requerimientos.GetValueOrDefault(detalle.CodigoItem) + detalle.Cantidad;
            }
        }

        foreach (var (sku, cantidad) in requerimientos)
        {
            var producto = await _productoRepository.ObtenerPorSkuAsync(sku, ct)
                ?? throw new DomainException($"Producto '{sku}' no encontrado durante descuento de stock.");

            producto.DescontarStock(cantidad);  // Lanza StockInsuficienteException si no alcanza
            _productoRepository.Actualizar(producto);

            var movimiento = new MovimientoInventario(
                producto.Id,
                producto.Sku,
                -cantidad,
                TipoMovimientoInventario.VENTA,
                $"Venta del pedido {pedido.NumeroPedido}",
                pedido.NumeroPedido
            );
            await _movimientoRepository.AgregarAsync(movimiento, ct);
        }
    }
}
