namespace LogiMarket360.Application.Patterns.TemplateMethod;

using LogiMarket360.Application.DTOs;
using LogiMarket360.Application.Patterns.Observer;
using LogiMarket360.Application.Patterns.Observer.Eventos;
using LogiMarket360.Application.Ports;
using LogiMarket360.Application.Ports.Repositories;
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
    protected readonly IProductoRepository _productoRepository;
    protected readonly IMovimientoInventarioRepository _movimientoRepository;
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly IEventoPublicador _eventoPublicador;

    public ProcesarDevolucionTemplate(
        IPedidoRepository pedidoRepository,
        IDevolucionRepository devolucionRepository,
        IProductoRepository productoRepository,
        IMovimientoInventarioRepository movimientoRepository,
        IUnitOfWork unitOfWork,
        IEventoPublicador eventoPublicador)
    {
        _pedidoRepository = pedidoRepository;
        _devolucionRepository = devolucionRepository;
        _productoRepository = productoRepository;
        _movimientoRepository = movimientoRepository;
        _unitOfWork = unitOfWork;
        _eventoPublicador = eventoPublicador;
    }

    // El método plantilla que orquesta los pasos invariantes
    public async Task<ResultadoDevolucion> ProcesarAsync(SolicitudDevolucion solicitud, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        // Paso 1: Verificar elegibilidad básica del pedido
        var pedido = await _pedidoRepository.ObtenerPorIdAsync(solicitud.PedidoId, ct);
        if (pedido == null)
        {
            return new ResultadoDevolucion(false, 0m, $"Pedido con ID '{solicitud.PedidoId}' no encontrado.", string.Empty);
        }

        if (pedido.EstadoCodigo != EstadoPedido.ENTREGADO)
        {
            return new ResultadoDevolucion(false, 0m, 
                $"Solo se pueden procesar devoluciones sobre pedidos en estado 'ENTREGADO'. Estado actual: '{pedido.EstadoCodigo}'.", string.Empty);
        }

        // Verificar que no exista devolución previa aprobada para este pedido (unicidad de devolución)
        var devolucionExistente = await _devolucionRepository.ObtenerPorPedidoIdAsync(pedido.Id, ct);
        if (devolucionExistente != null && devolucionExistente.Aprobada)
        {
            return new ResultadoDevolucion(false, 0m, 
                $"El pedido '{pedido.NumeroPedido}' ya cuenta con una devolución procesada previamente.", string.Empty);
        }

        // Paso 2: Inspeccionar condición específica según el motivo (paso abstracto/personalizado)
        var (cumpleCondicion, motivoRechazo) = InspeccionarCondicion(solicitud);
        if (!cumpleCondicion)
        {
            // Registrar devolución rechazada sin alterar stock ni generar reembolso
            var devRechazada = new Devolucion(
                pedido.Id,
                solicitud.Motivo,
                solicitud.DiasDesdeEntrega,
                solicitud.ProductoSinUso,
                solicitud.Detalle,
                0m
            );
            devRechazada.Rechazar(motivoRechazo);
            await _devolucionRepository.AgregarAsync(devRechazada, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return new ResultadoDevolucion(false, 0m, $"Devolución rechazada: {motivoRechazo}", string.Empty);
        }

        // Paso 3: Calcular reembolso (importe de los productos tras descuento, sin tarifa de envío)
        decimal montoReembolso = CalcularReembolso(pedido);

        // Paso 4: Registrar devolución y reintegrar existencias dentro de una transacción atómica
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            string referenciaReembolso = $"REF-REB-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

            var devolucionAprobada = new Devolucion(
                pedido.Id,
                solicitud.Motivo,
                solicitud.DiasDesdeEntrega,
                solicitud.ProductoSinUso,
                solicitud.Detalle,
                montoReembolso
            );
            devolucionAprobada.Aprobar(referenciaReembolso);
            await _devolucionRepository.AgregarAsync(devolucionAprobada, ct);

            // Reintegrar stock por cada SKU físico final
            await ReintegrarStockAsync(pedido, ct);

            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitAsync(ct);

            // Paso 5: Generar evento post-transaccional
            await GenerarEventoAsync(pedido, devolucionAprobada, ct);

            return new ResultadoDevolucion(true, montoReembolso, 
                $"Devolución aprobada exitosamente por USD {montoReembolso:F2}.", referenciaReembolso);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync(ct);
            return new ResultadoDevolucion(false, 0m, $"Error al registrar devolución: {ex.Message}", string.Empty);
        }
    }

    // Paso 2 abstracto: cada subclase evalúa sus reglas específicas
    protected abstract (bool Cumple, string Mensaje) InspeccionarCondicion(SolicitudDevolucion solicitud);

    // Paso 3: Regla invariante de cálculo de reembolso (Subtotal - Descuento, SIN flete)
    protected virtual decimal CalcularReembolso(Pedido pedido)
    {
        decimal reembolso = pedido.Subtotal - pedido.Descuento;
        return decimal.Round(reembolso, 2, MidpointRounding.AwayFromZero);
    }

    // Paso 4: Reintegrar existencias
    protected virtual async Task ReintegrarStockAsync(Pedido pedido, CancellationToken ct)
    {
        foreach (var detalle in pedido.Detalles)
        {
            if (detalle.ComponentesSnapshot.Count > 0)
            {
                foreach (var comp in detalle.ComponentesSnapshot)
                {
                    var prod = await _productoRepository.ObtenerPorSkuAsync(comp.Sku, ct)
                        ?? throw new DomainException($"Producto '{comp.Sku}' no encontrado al reintegrar stock.");

                    prod.ReintegrarStock(comp.CantidadFisica);
                    _productoRepository.Actualizar(prod);

                    var mov = new MovimientoInventario(
                        prod.Id,
                        prod.Sku,
                        comp.CantidadFisica,
                        TipoMovimientoInventario.DEVOLUCION,
                        $"Reintegro por devolución total de pedido {pedido.NumeroPedido}",
                        pedido.NumeroPedido
                    );
                    await _movimientoRepository.AgregarAsync(mov, ct);
                }
            }
            else
            {
                var prod = await _productoRepository.ObtenerPorSkuAsync(detalle.CodigoItem, ct)
                    ?? throw new DomainException($"Producto '{detalle.CodigoItem}' no encontrado al reintegrar stock.");

                prod.ReintegrarStock(detalle.Cantidad);
                _productoRepository.Actualizar(prod);

                var mov = new MovimientoInventario(
                    prod.Id,
                    prod.Sku,
                    detalle.Cantidad,
                    TipoMovimientoInventario.DEVOLUCION,
                    $"Reintegro por devolución total de pedido {pedido.NumeroPedido}",
                    pedido.NumeroPedido
                );
                await _movimientoRepository.AgregarAsync(mov, ct);
            }
        }
    }

    // Paso 5: Notificación y publicación de evento
    protected virtual async Task GenerarEventoAsync(Pedido pedido, Devolucion devolucion, CancellationToken ct)
    {
        await _eventoPublicador.PublicarAsync(new DevolucionAceptadaEvent(
            pedido.Id,
            devolucion.MontoReembolso,
            devolucion.Motivo.ToString(),
            DateTime.UtcNow
        ), ct);
    }
}
