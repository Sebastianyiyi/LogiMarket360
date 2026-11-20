namespace LogiMarket360.Application.Ports.Repositories;

using LogiMarket360.Domain.Entities;

public interface IKitRepository
{
    Task<Kit?> ObtenerPorCodigoAsync(string codigoKit, CancellationToken ct = default);
    Task<Kit?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Kit>> ListarTodosAsync(CancellationToken ct = default);
    Task AgregarAsync(Kit kit, CancellationToken ct = default);
    void Actualizar(Kit kit);
    void Eliminar(Kit kit);
}
