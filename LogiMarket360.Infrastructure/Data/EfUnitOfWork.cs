namespace LogiMarket360.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using LogiMarket360.Application.Exceptions;
using LogiMarket360.Application.Ports;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly LogiMarketDbContext _context;
    private IDbContextTransaction? _transaction;
    private bool _disposed = false;

    public EfUnitOfWork(LogiMarketDbContext context) => _context = context;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictoConcurrenciaException(
                "La operación no pudo completarse porque otro usuario modificó el stock o el pedido. Recargue los datos e intente nuevamente.",
                ex);
        }
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

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
        try
        {
            if (_transaction != null)
                await _transaction.RollbackAsync(ct);
        }
        finally
        {
            if (_transaction != null)
                await _transaction.DisposeAsync();
            _transaction = null;
            _context.ChangeTracker.Clear();
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
