namespace LogiMarket360.Application.Ports.Repositories;

using LogiMarket360.Domain.Entities;

public interface IEventoOutboxRepository
{
    Task AgregarAsync(EventoOutbox evento, CancellationToken ct = default);
    Task<IReadOnlyList<EventoOutbox>> ListarPendientesAsync(int limite = 50, CancellationToken ct = default);
    Task<IReadOnlyList<EventoOutbox>> ListarTodosAsync(CancellationToken ct = default);
    void Actualizar(EventoOutbox evento);
}
