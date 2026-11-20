namespace LogiMarket360.Application.Ports.Repositories;

using LogiMarket360.Domain.Entities;

public interface IDespachoRepository
{
    Task AgregarAsync(Despacho despacho, CancellationToken ct = default);
    Task<Despacho?> ObtenerPorPedidoIdAsync(int pedidoId, CancellationToken ct = default);
    Task<IReadOnlyList<Despacho>> ListarTodosAsync(CancellationToken ct = default);
    void Actualizar(Despacho despacho);
}
