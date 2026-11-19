namespace LogiMarket360.Application.Patterns.TemplateMethod;

using LogiMarket360.Application.Patterns.Observer;
using LogiMarket360.Application.Ports;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Domain.Enums;

public class DevolucionDefectoService : ProcesarDevolucionTemplate
{
    public DevolucionDefectoService(
        IPedidoRepository pedidoRepository,
        IDevolucionRepository devolucionRepository,
        IProductoRepository productoRepository,
        IMovimientoInventarioRepository movimientoRepository,
        IUnitOfWork unitOfWork,
        IEventoPublicador eventoPublicador)
        : base(pedidoRepository, devolucionRepository, productoRepository, movimientoRepository, unitOfWork, eventoPublicador)
    {
    }

    protected override (bool Cumple, string Mensaje) InspeccionarCondicion(SolicitudDevolucion solicitud)
    {
        if (solicitud.Motivo != MotivoDevolucion.DEFECTO)
        {
            return (false, "Este servicio solo procesa devoluciones por DEFECTO.");
        }

        // Política de negocio: DEFECTO se acepta hasta 30 días posteriores a la entrega
        if (solicitud.DiasDesdeEntrega > 30)
        {
            return (false, $"El plazo máximo para devoluciones por defecto es de 30 días. Días transcurridos: {solicitud.DiasDesdeEntrega}.");
        }

        return (true, "Condición de defecto aprobada dentro del plazo legal.");
    }
}

public class DevolucionArrepentimientoService : ProcesarDevolucionTemplate
{
    public DevolucionArrepentimientoService(
        IPedidoRepository pedidoRepository,
        IDevolucionRepository devolucionRepository,
        IProductoRepository productoRepository,
        IMovimientoInventarioRepository movimientoRepository,
        IUnitOfWork unitOfWork,
        IEventoPublicador eventoPublicador)
        : base(pedidoRepository, devolucionRepository, productoRepository, movimientoRepository, unitOfWork, eventoPublicador)
    {
    }

    protected override (bool Cumple, string Mensaje) InspeccionarCondicion(SolicitudDevolucion solicitud)
    {
        if (solicitud.Motivo != MotivoDevolucion.ARREPENTIMIENTO)
        {
            return (false, "Este servicio solo procesa devoluciones por ARREPENTIMIENTO.");
        }

        // Política de negocio: ARREPENTIMIENTO se acepta hasta 7 días y producto sin uso
        if (solicitud.DiasDesdeEntrega > 7)
        {
            return (false, $"El plazo máximo por arrepentimiento es de 7 días. Días transcurridos: {solicitud.DiasDesdeEntrega}.");
        }

        if (!solicitud.ProductoSinUso)
        {
            return (false, "La devolución por arrepentimiento exige que el producto esté sin uso.");
        }

        return (true, "Condición de arrepentimiento aprobada (dentro de 7 días y producto sin uso).");
    }
}
