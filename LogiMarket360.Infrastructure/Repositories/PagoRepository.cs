namespace LogiMarket360.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Domain.Entities;
using LogiMarket360.Infrastructure.Data;

public class PagoRepository : IPagoRepository
{
    private readonly LogiMarketDbContext _context;
    public PagoRepository(LogiMarketDbContext context) => _context = context;

    public async Task AgregarAsync(Pago pago, CancellationToken ct = default)
        => await _context.Pagos.AddAsync(pago, ct);

    public async Task<Pago?> ObtenerPorClaveIdempotenciaAsync(string claveIdempotencia, CancellationToken ct = default)
        => await _context.Pagos.FirstOrDefaultAsync(p => p.ClaveIdempotencia == claveIdempotencia, ct);

    public async Task<Pago?> ObtenerPorPedidoIdAsync(int pedidoId, CancellationToken ct = default)
        => await _context.Pagos
            .Where(p => p.PedidoId == pedidoId)
            .OrderByDescending(p => p.FechaPago)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Pago>> ListarTodosAsync(CancellationToken ct = default)
        => await _context.Pagos.OrderByDescending(p => p.FechaPago).ToListAsync(ct);
}
