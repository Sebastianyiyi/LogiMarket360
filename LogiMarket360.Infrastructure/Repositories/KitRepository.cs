namespace LogiMarket360.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Domain.Entities;
using LogiMarket360.Infrastructure.Data;

public class KitRepository : IKitRepository
{
    private readonly LogiMarketDbContext _context;
    public KitRepository(LogiMarketDbContext context) => _context = context;

    public async Task<Kit?> ObtenerPorCodigoAsync(string codigoKit, CancellationToken ct = default)
        => await _context.Kits.Include(k => k.Componentes)
            .FirstOrDefaultAsync(k => k.CodigoKit == codigoKit.ToUpperInvariant(), ct);

    public async Task<Kit?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        => await _context.Kits.Include(k => k.Componentes)
            .FirstOrDefaultAsync(k => k.Id == id, ct);

    public async Task<IReadOnlyList<Kit>> ListarTodosAsync(CancellationToken ct = default)
        => await _context.Kits.Include(k => k.Componentes).ToListAsync(ct);

    public async Task AgregarAsync(Kit kit, CancellationToken ct = default)
        => await _context.Kits.AddAsync(kit, ct);
}
