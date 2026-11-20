namespace LogiMarket360.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Domain.Entities;
using LogiMarket360.Domain.Enums;
using LogiMarket360.Infrastructure.Data;

public class PedidoRepository : IPedidoRepository
{
    private readonly LogiMarketDbContext _context;
    public PedidoRepository(LogiMarketDbContext context) => _context = context;

    private IQueryable<Pedido> QueryConIncludes()
        => _context.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Detalles)
                .ThenInclude(d => d.ComponentesSnapshot);

    public async Task<Pedido?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        var pedido = await QueryConIncludes().FirstOrDefaultAsync(p => p.Id == id, ct);
        pedido?.SincronizarEstadoDesdePersistencia();
        return pedido;
    }

    public async Task<Pedido?> ObtenerPorNumeroAsync(string numeroPedido, CancellationToken ct = default)
    {
        var pedido = await QueryConIncludes()
            .FirstOrDefaultAsync(p => p.NumeroPedido == numeroPedido.ToUpperInvariant(), ct);
        pedido?.SincronizarEstadoDesdePersistencia();
        return pedido;
    }

    public async Task<Pedido?> ObtenerPorIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default)
    {
        var pedido = await QueryConIncludes()
            .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, ct);
        pedido?.SincronizarEstadoDesdePersistencia();
        return pedido;
    }

    public async Task<IReadOnlyList<Pedido>> ListarTodosAsync(CancellationToken ct = default)
    {
        var pedidos = await QueryConIncludes().OrderByDescending(p => p.FechaCreacion).ToListAsync(ct);
        pedidos.ForEach(p => p.SincronizarEstadoDesdePersistencia());
        return pedidos;
    }

    public async Task<IReadOnlyList<Pedido>> ListarPorEstadoAsync(EstadoPedido estado, CancellationToken ct = default)
    {
        var pedidos = await QueryConIncludes()
            .Where(p => p.EstadoCodigo == estado)
            .ToListAsync(ct);
        pedidos.ForEach(p => p.SincronizarEstadoDesdePersistencia());
        return pedidos;
    }

    public async Task AgregarAsync(Pedido pedido, CancellationToken ct = default)
        => await _context.Pedidos.AddAsync(pedido, ct);

    public void Actualizar(Pedido pedido)
        => _context.Pedidos.Update(pedido);
}
