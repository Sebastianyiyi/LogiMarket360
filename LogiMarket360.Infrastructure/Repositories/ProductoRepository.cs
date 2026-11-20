namespace LogiMarket360.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Domain.Entities;
using LogiMarket360.Infrastructure.Data;

public class ProductoRepository : IProductoRepository
{
    private readonly LogiMarketDbContext _context;
    public ProductoRepository(LogiMarketDbContext context) => _context = context;

    public async Task<Producto?> ObtenerPorSkuAsync(string sku, CancellationToken ct = default)
        => await _context.Productos.FirstOrDefaultAsync(p => p.Sku == sku.ToUpperInvariant(), ct);

    public async Task<Producto?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        => await _context.Productos.FindAsync(new object[] { id }, ct);

    public async Task<IReadOnlyList<Producto>> ListarTodosAsync(CancellationToken ct = default)
        => await _context.Productos.ToListAsync(ct);

    public async Task<IReadOnlyList<Producto>> BuscarAsync(string criterio, CancellationToken ct = default)
    {
        string c = criterio.ToUpperInvariant();
        return await _context.Productos
            .Where(p => p.Sku.Contains(c) || p.Nombre.Contains(criterio))
            .ToListAsync(ct);
    }

    public async Task AgregarAsync(Producto producto, CancellationToken ct = default)
        => await _context.Productos.AddAsync(producto, ct);

    public void Actualizar(Producto producto)
        => _context.Productos.Update(producto);

    public void Eliminar(Producto producto)
        => _context.Productos.Remove(producto);
}
