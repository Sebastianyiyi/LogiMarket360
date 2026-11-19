namespace LogiMarket360.Application.Ports.Repositories;

using LogiMarket360.Domain.Entities;

public interface IAuditoriaRepository
{
    Task AgregarAsync(Auditoria auditoria, CancellationToken ct = default);
    Task<IReadOnlyList<Auditoria>> ListarRecientesAsync(int limite = 100, CancellationToken ct = default);
}
