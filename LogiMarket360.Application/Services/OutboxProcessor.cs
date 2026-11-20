namespace LogiMarket360.Application.Services;

using System.Text.Json;
using LogiMarket360.Application.Patterns.Observer;
using LogiMarket360.Application.Patterns.Observer.Eventos;
using LogiMarket360.Application.Ports;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Domain.Entities;

public class OutboxProcessor : IOutboxProcesador
{
    private readonly IEventoOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventoPublicador _eventoPublicador;
    private readonly INotificadorFactory _notificadorFactory;

    public OutboxProcessor(
        IEventoOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork,
        IEventoPublicador eventoPublicador,
        INotificadorFactory notificadorFactory)
    {
        _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _eventoPublicador = eventoPublicador ?? throw new ArgumentNullException(nameof(eventoPublicador));
        _notificadorFactory = notificadorFactory ?? throw new ArgumentNullException(nameof(notificadorFactory));
    }

    public async Task<int> ProcesarPendientesAsync(int limite = 50, CancellationToken ct = default)
    {
        var pendientes = await _outboxRepository.ListarPendientesAsync(limite, ct);
        int procesados = 0;

        foreach (var evento in pendientes)
        {
            try
            {
                await ProcesarEventoAsync(evento, ct);
                evento.MarcarProcesado();
                _outboxRepository.Actualizar(evento);
                await _unitOfWork.SaveChangesAsync(ct);
                procesados++;
            }
            catch (Exception ex)
            {
                evento.RegistrarFallo(ex.Message);
                _outboxRepository.Actualizar(evento);
                await _unitOfWork.SaveChangesAsync(ct);
            }
        }

        return procesados;
    }

    private async Task ProcesarEventoAsync(EventoOutbox evento, CancellationToken ct)
    {
        using var documento = JsonDocument.Parse(evento.ContenidoJson);
        var raiz = documento.RootElement;

        switch (evento.TipoEvento)
        {
            case "PedidoPagado":
            {
                int pedidoId = raiz.GetProperty("pedidoId").GetInt32();
                string numeroPedido = raiz.GetProperty("numeroPedido").GetString() ?? string.Empty;
                int clienteId = raiz.TryGetProperty("clienteId", out var c) ? c.GetInt32() : 0;
                decimal total = raiz.GetProperty("total").GetDecimal();
                string referencia = raiz.GetProperty("referencia").GetString() ?? string.Empty;
                DateTime fecha = raiz.TryGetProperty("fecha", out var f) ? f.GetDateTime() : DateTime.UtcNow;

                await _eventoPublicador.PublicarAsync(new PedidoPagadoEvent(
                    pedidoId, numeroPedido, clienteId, total, referencia, fecha), ct);

                await NotificarAsync(
                    clienteId > 0 ? $"cliente-{clienteId}" : $"pedido-{pedidoId}",
                    $"Pedido {numeroPedido} pagado por USD {total:F2}. Referencia: {referencia}.",
                    ct);
                break;
            }

            case "DespachoRegistrado":
            {
                int pedidoId = raiz.GetProperty("pedidoId").GetInt32();
                string numeroGuia = raiz.GetProperty("numeroGuia").GetString() ?? string.Empty;
                string proveedor = raiz.GetProperty("proveedor").GetString() ?? string.Empty;
                DateTime fecha = raiz.TryGetProperty("fecha", out var f) ? f.GetDateTime() : DateTime.UtcNow;

                await _eventoPublicador.PublicarAsync(new DespachoRegistradoEvent(
                    pedidoId, numeroGuia, proveedor, fecha), ct);

                await NotificarAsync(
                    $"pedido-{pedidoId}",
                    $"Pedido {pedidoId} despachado. Guía: {numeroGuia}. Proveedor: {proveedor}.",
                    ct);
                break;
            }

            case "DevolucionAceptada":
            {
                int pedidoId = raiz.GetProperty("pedidoId").GetInt32();
                decimal monto = raiz.GetProperty("monto").GetDecimal();
                string motivo = raiz.GetProperty("motivo").GetString() ?? string.Empty;
                DateTime fecha = raiz.TryGetProperty("fecha", out var f) ? f.GetDateTime() : DateTime.UtcNow;

                await _eventoPublicador.PublicarAsync(new DevolucionAceptadaEvent(
                    pedidoId, monto, motivo, fecha), ct);

                await NotificarAsync(
                    $"pedido-{pedidoId}",
                    $"Devolución aceptada del pedido {pedidoId}. Reembolso: USD {monto:F2}. Motivo: {motivo}.",
                    ct);
                break;
            }

            default:
                throw new InvalidOperationException($"Tipo de evento Outbox no soportado: '{evento.TipoEvento}'.");
        }
    }

    private async Task NotificarAsync(string destinatario, string mensaje, CancellationToken ct)
    {
        var notificador = _notificadorFactory.Crear(_notificadorFactory.CanalPorDefecto);
        bool enviado = await notificador.EnviarAsync(destinatario, mensaje, ct);
        if (!enviado)
            throw new InvalidOperationException($"El notificador '{notificador.Canal}' no pudo enviar el aviso a '{destinatario}'.");
    }
}
