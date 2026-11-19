namespace LogiMarket360.Application.Ports.Repositories;

using LogiMarket360.Domain.Entities;

public interface IMovimientoInventarioRepository
{
    Task AgregarAsync(MovimientoInventario movimiento, CancellationToken ct = default);
    Task<IReadOnlyList<MovimientoInventario>> ListarPorProductoAsync(int productoId, CancellationToken ct = default);
    Task<IReadOnlyList<MovimientoInventario>> ListarTodosAsync(CancellationToken ct = default);
}
