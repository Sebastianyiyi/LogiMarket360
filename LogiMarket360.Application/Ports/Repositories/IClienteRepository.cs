namespace LogiMarket360.Application.Ports.Repositories;

using LogiMarket360.Domain.Entities;

public interface IClienteRepository
{
    Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<Cliente?> ObtenerPorIdentificacionAsync(string identificacion, CancellationToken ct = default);
    Task<IReadOnlyList<Cliente>> ListarTodosAsync(CancellationToken ct = default);
    Task AgregarAsync(Cliente cliente, CancellationToken ct = default);
    void Actualizar(Cliente cliente);
}
