namespace LogiMarket360.Application.Ports.Repositories;

using LogiMarket360.Domain.Entities;

public interface IProductoRepository
{
    Task<Producto?> ObtenerPorSkuAsync(string sku, CancellationToken ct = default);
    Task<Producto?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Producto>> ListarTodosAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Producto>> BuscarAsync(string criterio, CancellationToken ct = default);
    Task AgregarAsync(Producto producto, CancellationToken ct = default);
    void Actualizar(Producto producto);
}
