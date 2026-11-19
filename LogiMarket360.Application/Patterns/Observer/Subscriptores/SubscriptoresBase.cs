namespace LogiMarket360.Application.Patterns.Observer.Subscriptores;

using LogiMarket360.Application.Patterns.Observer.Eventos;
using LogiMarket360.Application.Patterns.Singleton;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Domain.Entities;

public class MetricasObserver : 
    IEventoSubscriptor<PedidoPagadoEvent>,
    IEventoSubscriptor<DevolucionAceptadaEvent>
{
    private readonly MetricasSesion _metricas;

    public MetricasObserver(MetricasSesion metricas)
    {
        _metricas = metricas ?? throw new ArgumentNullException(nameof(metricas));
    }

    public Task ManejarAsync(PedidoPagadoEvent evento, CancellationToken ct = default)
    {
        _metricas.RegistrarVenta(evento.TotalPagado);
        return Task.CompletedTask;
    }

    public Task ManejarAsync(DevolucionAceptadaEvent evento, CancellationToken ct = default)
    {
        _metricas.RegistrarDevolucion(evento.MontoReembolsado);
        return Task.CompletedTask;
    }
}

public class AuditoriaObserver :
    IEventoSubscriptor<PedidoPagadoEvent>,
    IEventoSubscriptor<DespachoRegistradoEvent>,
    IEventoSubscriptor<DevolucionAceptadaEvent>
{
    private readonly IAuditoriaRepository _auditoriaRepository;

    public AuditoriaObserver(IAuditoriaRepository auditoriaRepository)
    {
        _auditoriaRepository = auditoriaRepository ?? throw new ArgumentNullException(nameof(auditoriaRepository));
    }

    public async Task ManejarAsync(PedidoPagadoEvent evento, CancellationToken ct = default)
    {
        var audit = new Auditoria(
            usuario: "SISTEMA",
            operacion: "PAGO_CONFIRMADO",
            entidad: "Pedido",
            entidadId: evento.PedidoId.ToString(),
            detalles: $"Total: USD {evento.TotalPagado:F2} | Ref: {evento.ReferenciaPago}",
            exitoso: true
        );
        await _auditoriaRepository.AgregarAsync(audit, ct);
    }

    public async Task ManejarAsync(DespachoRegistradoEvent evento, CancellationToken ct = default)
    {
        var audit = new Auditoria(
            usuario: "BODEGA",
            operacion: "DESPACHO_REGISTRADO",
            entidad: "Despacho",
            entidadId: evento.PedidoId.ToString(),
            detalles: $"Guía: {evento.NumeroGuia} | Proveedor: {evento.ProveedorLogistico}",
            exitoso: true
        );
        await _auditoriaRepository.AgregarAsync(audit, ct);
    }

    public async Task ManejarAsync(DevolucionAceptadaEvent evento, CancellationToken ct = default)
    {
        var audit = new Auditoria(
            usuario: "SISTEMA",
            operacion: "DEVOLUCION_ACEPTADA",
            entidad: "Devolucion",
            entidadId: evento.PedidoId.ToString(),
            detalles: $"Reembolso: USD {evento.MontoReembolsado:F2} | Motivo: {evento.Motivo}",
            exitoso: true
        );
        await _auditoriaRepository.AgregarAsync(audit, ct);
    }
}
