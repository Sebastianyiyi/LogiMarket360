namespace LogiMarket360.Application.Patterns.TemplateMethod;

using System.Text.Json;
using LogiMarket360.Application.Exceptions;
using LogiMarket360.Application.Ports;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Application.Security;
using LogiMarket360.Domain.Entities;
using LogiMarket360.Domain.Enums;
using LogiMarket360.Domain.Exceptions;

public record SolicitudDevolucion(
    int PedidoId,
    MotivoDevolucion Motivo,
    int DiasDesdeEntrega,
    bool ProductoSinUso,
    string Detalle
);

public record ResultadoDevolucion(
    bool Aprobada,
    decimal MontoReembolsado,
    string Mensaje,
    string ReferenciaReembolso
);

public abstract class ProcesarDevolucionTemplate
{
    protected readonly IPedidoRepository _pedidoRepository;
    protected readonly IDevolucionRepository _devolucionRepository;
    protected readonly IDespachoRepository _despachoRepository;
    protected readonly IProductoRepository _productoRepository;
    protected readonly IMovimientoInventarioRepository _movimientoRepository;
    protected readonly IAuditoriaRepository _auditoriaRepository;
    protected readonly IEventoOutboxRepository _outboxRepository;
    protected readonly IOutboxProcesador _outboxProcesador;
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly IAutorizador _autorizador;

    protected ProcesarDevolucionTemplate(
        IPedidoRepository pedidoRepository,
        IDevolucionRepository devolucionRepository,
        IDespachoRepository despachoRepository,
        IProductoRepository productoRepository,
        IMovimientoInventarioRepository movimientoRepository,
        IAuditoriaRepository auditoriaRepository,
        IEventoOutboxRepository outboxRepository,
        IOutboxProcesador outboxProcesador,
        IUnitOfWork unitOfWork,
        IAutorizador autorizador)
    {
        _pedidoRepository = pedidoRepository;
        _devolucionRepository = devolucionRepository;
        _despachoRepository = despachoRepository;
        _productoRepository = productoRepository;
        _movimientoRepository = movimientoRepository;
        _auditoriaRepository = auditoriaRepository;
        _outboxRepository = outboxRepository;
        _outboxProcesador = outboxProcesador;
        _unitOfWork = unitOfWork;
        _autorizador = autorizador;
    }

    public async Task<ResultadoDevolucion> ProcesarAsync(SolicitudDevolucion solicitud, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        try
        {
            _autorizador.ExigirRol(Permisos.Devolucion);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ResultadoDevolucion(false, 0m, ex.Message, string.Empty);
        }

        var pedido = await _pedidoRepository.ObtenerPorIdAsync(solicitud.PedidoId, ct);
        if (pedido == null)
        {
            return new ResultadoDevolucion(false, 0m, $"Pedido con ID '{solicitud.PedidoId}' no encontrado.", string.Empty);
        }

        if (pedido.EstadoCodigo != EstadoPedido.ENTREGADO)
        {
            return new ResultadoDevolucion(false, 0m,
                $"Solo se pueden procesar devoluciones sobre pedidos en estado 'ENTREGADO'. Estado actual: '{pedido.EstadoCodigo}'.",
                string.Empty);
        }

        var devolucionExistente = await _devolucionRepository.ObtenerPorPedidoIdAsync(pedido.Id, ct);
        if (devolucionExistente is not null && devolucionExistente.Aprobada)
        {
            return new ResultadoDevolucion(false, 0m,
                $"El pedido '{pedido.NumeroPedido}' ya cuenta con una devolución aprobada previamente.",
                devolucionExistente.ReferenciaReembolso);
        }

        var despacho = await _despachoRepository.ObtenerPorPedidoIdAsync(pedido.Id, ct);
        int diasCalculados = despacho?.FechaEntregaReal is DateTime fechaEntrega
            ? Math.Max(0, (DateTime.UtcNow - fechaEntrega).Days)
            : 0;
        // El valor de la solicitud permite reproducir escenarios controlados y nunca
        // puede reducir el tiempo real ya transcurrido desde la entrega.
        int diasDesdeEntrega = Math.Max(diasCalculados, solicitud.DiasDesdeEntrega);

        var solicitudEvaluada = solicitud with { DiasDesdeEntrega = diasDesdeEntrega };
        var (cumpleCondicion, motivoRechazo) = InspeccionarCondicion(solicitudEvaluada);

        if (!cumpleCondicion)
        {
            return await RegistrarRechazoAsync(pedido, devolucionExistente, solicitudEvaluada, motivoRechazo, ct);
        }

        decimal montoReembolso = CalcularReembolso(pedido);
        string referenciaReembolso = $"REF-REB-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            Devolucion devolucion;
            if (devolucionExistente is not null)
            {
                devolucionExistente.ReiniciarSolicitud(
                    solicitudEvaluada.Motivo,
                    solicitudEvaluada.DiasDesdeEntrega,
                    solicitudEvaluada.ProductoSinUso,
                    solicitudEvaluada.Detalle,
                    montoReembolso);
                devolucionExistente.Aprobar(referenciaReembolso);
                _devolucionRepository.Actualizar(devolucionExistente);
                devolucion = devolucionExistente;
            }
            else
            {
                devolucion = new Devolucion(
                    pedido.Id,
                    solicitudEvaluada.Motivo,
                    solicitudEvaluada.DiasDesdeEntrega,
                    solicitudEvaluada.ProductoSinUso,
                    solicitudEvaluada.Detalle,
                    montoReembolso);
                devolucion.Aprobar(referenciaReembolso);
                await _devolucionRepository.AgregarAsync(devolucion, ct);
            }

            await ReintegrarStockAsync(pedido, ct);

            var payload = JsonSerializer.Serialize(new
            {
                pedidoId = pedido.Id,
                monto = montoReembolso,
                motivo = solicitudEvaluada.Motivo.ToString(),
                fecha = DateTime.UtcNow
            });
            await _outboxRepository.AgregarAsync(new EventoOutbox("DevolucionAceptada", payload), ct);

            await _auditoriaRepository.AgregarAsync(new Auditoria(
                usuario: "SISTEMA",
                operacion: "DEVOLUCION_APROBADA",
                entidad: "Devolucion",
                entidadId: pedido.Id.ToString(),
                detalles: $"Motivo: {solicitudEvaluada.Motivo} | Reembolso: USD {montoReembolso:F2} | Ref: {referenciaReembolso}",
                exitoso: true), ct);

            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitAsync(ct);
        }
        catch (Exception ex) when (ex is ConflictoConcurrenciaException or DomainException or InvalidOperationException)
        {
            await _unitOfWork.RollbackAsync(ct);
            return new ResultadoDevolucion(false, 0m, $"Error al registrar devolución: {ex.Message}", string.Empty);
        }

        await _outboxProcesador.ProcesarPendientesAsync(50, ct);

        return new ResultadoDevolucion(
            true,
            montoReembolso,
            $"Devolución aprobada exitosamente por USD {montoReembolso:F2}.",
            referenciaReembolso);
    }

    private async Task<ResultadoDevolucion> RegistrarRechazoAsync(
        Pedido pedido,
        Devolucion? devolucionExistente,
        SolicitudDevolucion solicitud,
        string motivoRechazo,
        CancellationToken ct)
    {
        if (devolucionExistente is not null)
        {
            devolucionExistente.ReiniciarSolicitud(
                solicitud.Motivo,
                solicitud.DiasDesdeEntrega,
                solicitud.ProductoSinUso,
                solicitud.Detalle,
                0m);
            devolucionExistente.Rechazar(motivoRechazo);
            _devolucionRepository.Actualizar(devolucionExistente);
        }
        else
        {
            var devolucionRechazada = new Devolucion(
                pedido.Id,
                solicitud.Motivo,
                solicitud.DiasDesdeEntrega,
                solicitud.ProductoSinUso,
                solicitud.Detalle,
                0m);
            devolucionRechazada.Rechazar(motivoRechazo);
            await _devolucionRepository.AgregarAsync(devolucionRechazada, ct);
        }

        await _auditoriaRepository.AgregarAsync(new Auditoria(
            usuario: "SISTEMA",
            operacion: "DEVOLUCION_RECHAZADA",
            entidad: "Devolucion",
            entidadId: pedido.Id.ToString(),
            detalles: motivoRechazo,
            exitoso: false), ct);

        await _unitOfWork.SaveChangesAsync(ct);
        return new ResultadoDevolucion(false, 0m, $"Devolución rechazada: {motivoRechazo}", string.Empty);
    }

    protected abstract (bool Cumple, string Mensaje) InspeccionarCondicion(SolicitudDevolucion solicitud);

    protected virtual decimal CalcularReembolso(Pedido pedido)
    {
        decimal reembolso = pedido.Subtotal - pedido.Descuento;
        return decimal.Round(reembolso, 2, MidpointRounding.AwayFromZero);
    }

    protected virtual async Task ReintegrarStockAsync(Pedido pedido, CancellationToken ct)
    {
        foreach (var detalle in pedido.Detalles)
        {
            if (detalle.ComponentesSnapshot.Count > 0)
            {
                foreach (var comp in detalle.ComponentesSnapshot)
                {
                    var producto = await _productoRepository.ObtenerPorSkuAsync(comp.Sku, ct)
                        ?? throw new DomainException($"Producto '{comp.Sku}' no encontrado al reintegrar stock.");

                    producto.ReintegrarStock(comp.CantidadFisica);
                    _productoRepository.Actualizar(producto);

                    await _movimientoRepository.AgregarAsync(new MovimientoInventario(
                        producto.Id,
                        producto.Sku,
                        comp.CantidadFisica,
                        TipoMovimientoInventario.DEVOLUCION,
                        $"Reintegro por devolución total de pedido {pedido.NumeroPedido}",
                        pedido.NumeroPedido), ct);
                }
            }
            else
            {
                var producto = await _productoRepository.ObtenerPorSkuAsync(detalle.CodigoItem, ct)
                    ?? throw new DomainException($"Producto '{detalle.CodigoItem}' no encontrado al reintegrar stock.");

                producto.ReintegrarStock(detalle.Cantidad);
                _productoRepository.Actualizar(producto);

                await _movimientoRepository.AgregarAsync(new MovimientoInventario(
                    producto.Id,
                    producto.Sku,
                    detalle.Cantidad,
                    TipoMovimientoInventario.DEVOLUCION,
                    $"Reintegro por devolución total de pedido {pedido.NumeroPedido}",
                    pedido.NumeroPedido), ct);
            }
        }
    }
}
