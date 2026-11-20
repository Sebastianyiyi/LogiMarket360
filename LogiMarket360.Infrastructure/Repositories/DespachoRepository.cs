namespace LogiMarket360.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Domain.Entities;
using LogiMarket360.Infrastructure.Data;

public class DespachoRepository : IDespachoRepository
{
    private readonly LogiMarketDbContext _context;
    public DespachoRepository(LogiMarketDbContext context) => _context = context;

    public async Task AgregarAsync(Despacho despacho, CancellationToken ct = default)
        => await _context.Despachos.AddAsync(despacho, ct);

    public async Task<Despacho?> ObtenerPorPedidoIdAsync(int pedidoId, CancellationToken ct = default)
        => await _context.Despachos
            .Where(d => d.PedidoId == pedidoId)
            .OrderByDescending(d => d.FechaDespacho)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Despacho>> ListarTodosAsync(CancellationToken ct = default)
        => await _context.Despachos.OrderByDescending(d => d.FechaDespacho).ToListAsync(ct);

    public void Actualizar(Despacho despacho)
        => _context.Despachos.Update(despacho);
}
