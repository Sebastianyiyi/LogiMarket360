namespace LogiMarket360.Application.Patterns.TemplateMethod;

using LogiMarket360.Application.Ports;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Application.Security;
using LogiMarket360.Domain.Enums;

public class DevolucionDefectoService : ProcesarDevolucionTemplate
{
    public DevolucionDefectoService(
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
        : base(pedidoRepository, devolucionRepository, despachoRepository, productoRepository,
               movimientoRepository, auditoriaRepository, outboxRepository, outboxProcesador, unitOfWork, autorizador)
    {
    }

    protected override (bool Cumple, string Mensaje) InspeccionarCondicion(SolicitudDevolucion solicitud)
    {
        if (solicitud.Motivo != MotivoDevolucion.DEFECTO)
            return (false, "Este servicio solo procesa devoluciones por DEFECTO.");

        if (solicitud.DiasDesdeEntrega > 30)
        {
            return (false,
                $"El plazo máximo para devoluciones por defecto es de 30 días. Días transcurridos: {solicitud.DiasDesdeEntrega}.");
        }

        return (true, "Condición de defecto aprobada dentro del plazo legal.");
    }
}

public class DevolucionArrepentimientoService : ProcesarDevolucionTemplate
{
    public DevolucionArrepentimientoService(
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
        : base(pedidoRepository, devolucionRepository, despachoRepository, productoRepository,
               movimientoRepository, auditoriaRepository, outboxRepository, outboxProcesador, unitOfWork, autorizador)
    {
    }

    protected override (bool Cumple, string Mensaje) InspeccionarCondicion(SolicitudDevolucion solicitud)
    {
        if (solicitud.Motivo != MotivoDevolucion.ARREPENTIMIENTO)
            return (false, "Este servicio solo procesa devoluciones por ARREPENTIMIENTO.");

        if (solicitud.DiasDesdeEntrega > 7)
        {
            return (false,
                $"El plazo máximo por arrepentimiento es de 7 días. Días transcurridos: {solicitud.DiasDesdeEntrega}.");
        }

        if (!solicitud.ProductoSinUso)
            return (false, "La devolución por arrepentimiento exige que el producto esté sin uso.");

        return (true, "Condición de arrepentimiento aprobada (dentro de 7 días y producto sin uso).");
    }
}
