namespace LogiMarket360.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using LogiMarket360.Application.Ports;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly LogiMarketDbContext _context;
    private IDbContextTransaction? _transaction;
    private bool _disposed = false;

    public EfUnitOfWork(LogiMarketDbContext context) => _context = context;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _transaction = await _context.Database.BeginTransactionAsync(ct);

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _transaction?.Dispose();
            _context.Dispose();
            _disposed = true;
        }
    }
}
