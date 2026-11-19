namespace LogiMarket360.Application.Ports.Repositories;

using LogiMarket360.Domain.Entities;

public interface IDevolucionRepository
{
    Task<Devolucion?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<Devolucion?> ObtenerPorPedidoIdAsync(int pedidoId, CancellationToken ct = default);
    Task<IReadOnlyList<Devolucion>> ListarTodasAsync(CancellationToken ct = default);
    Task AgregarAsync(Devolucion devolucion, CancellationToken ct = default);
    void Actualizar(Devolucion devolucion);
}
