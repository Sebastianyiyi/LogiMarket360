namespace LogiMarket360.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Domain.Entities;
using LogiMarket360.Infrastructure.Data;

public class ClienteRepository : IClienteRepository
{
    private readonly LogiMarketDbContext _context;
    public ClienteRepository(LogiMarketDbContext context) => _context = context;

    public async Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        => await _context.Clientes.FindAsync(new object[] { id }, ct);

    public async Task<Cliente?> ObtenerPorIdentificacionAsync(string identificacion, CancellationToken ct = default)
        => await _context.Clientes.FirstOrDefaultAsync(c => c.Identificacion == identificacion, ct);

    public async Task<IReadOnlyList<Cliente>> ListarTodosAsync(CancellationToken ct = default)
        => await _context.Clientes.ToListAsync(ct);

    public async Task AgregarAsync(Cliente cliente, CancellationToken ct = default)
        => await _context.Clientes.AddAsync(cliente, ct);

    public void Actualizar(Cliente cliente)
        => _context.Clientes.Update(cliente);
}

public class UsuarioRepository : IUsuarioRepository
{
    private readonly LogiMarketDbContext _context;
    public UsuarioRepository(LogiMarketDbContext context) => _context = context;

    public async Task<Usuario?> ObtenerPorUsernameAsync(string username, CancellationToken ct = default)
        => await _context.Usuarios.FirstOrDefaultAsync(u => u.Username == username.ToLowerInvariant(), ct);

    public async Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        => await _context.Usuarios.FindAsync(new object[] { id }, ct);

    public async Task<IReadOnlyList<Usuario>> ListarTodosAsync(CancellationToken ct = default)
        => await _context.Usuarios.ToListAsync(ct);

    public async Task AgregarAsync(Usuario usuario, CancellationToken ct = default)
        => await _context.Usuarios.AddAsync(usuario, ct);

    public void Actualizar(Usuario usuario)
        => _context.Usuarios.Update(usuario);
}

public class MovimientoInventarioRepository : IMovimientoInventarioRepository
{
    private readonly LogiMarketDbContext _context;
    public MovimientoInventarioRepository(LogiMarketDbContext context) => _context = context;

    public async Task AgregarAsync(MovimientoInventario movimiento, CancellationToken ct = default)
        => await _context.MovimientosInventario.AddAsync(movimiento, ct);

    public async Task<IReadOnlyList<MovimientoInventario>> ListarPorProductoAsync(int productoId, CancellationToken ct = default)
        => await _context.MovimientosInventario
            .Where(m => m.ProductoId == productoId)
            .OrderByDescending(m => m.Fecha)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<MovimientoInventario>> ListarTodosAsync(CancellationToken ct = default)
        => await _context.MovimientosInventario.OrderByDescending(m => m.Fecha).ToListAsync(ct);
}

public class DevolucionRepository : IDevolucionRepository
{
    private readonly LogiMarketDbContext _context;
    public DevolucionRepository(LogiMarketDbContext context) => _context = context;

    public async Task<Devolucion?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        => await _context.Devoluciones.FindAsync(new object[] { id }, ct);

    public async Task<Devolucion?> ObtenerPorPedidoIdAsync(int pedidoId, CancellationToken ct = default)
        => await _context.Devoluciones.FirstOrDefaultAsync(d => d.PedidoId == pedidoId, ct);

    public async Task<IReadOnlyList<Devolucion>> ListarTodasAsync(CancellationToken ct = default)
        => await _context.Devoluciones.OrderByDescending(d => d.FechaSolicitud).ToListAsync(ct);

    public async Task AgregarAsync(Devolucion devolucion, CancellationToken ct = default)
        => await _context.Devoluciones.AddAsync(devolucion, ct);

    public void Actualizar(Devolucion devolucion)
        => _context.Devoluciones.Update(devolucion);
}

public class AuditoriaRepository : IAuditoriaRepository
{
    private readonly LogiMarketDbContext _context;
    public AuditoriaRepository(LogiMarketDbContext context) => _context = context;

    public async Task AgregarAsync(Auditoria auditoria, CancellationToken ct = default)
        => await _context.Auditorias.AddAsync(auditoria, ct);

    public async Task<IReadOnlyList<Auditoria>> ListarRecientesAsync(int limite = 100, CancellationToken ct = default)
        => await _context.Auditorias.OrderByDescending(a => a.Fecha).Take(limite).ToListAsync(ct);
}

public class EventoOutboxRepository : IEventoOutboxRepository
{
    private readonly LogiMarketDbContext _context;
    public EventoOutboxRepository(LogiMarketDbContext context) => _context = context;

    public async Task AgregarAsync(EventoOutbox evento, CancellationToken ct = default)
        => await _context.EventosOutbox.AddAsync(evento, ct);

    public async Task<IReadOnlyList<EventoOutbox>> ListarPendientesAsync(int limite = 50, CancellationToken ct = default)
        => await _context.EventosOutbox
            .Where(e => !e.Procesado)
            .OrderBy(e => e.FechaCreacion)
            .Take(limite)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EventoOutbox>> ListarTodosAsync(CancellationToken ct = default)
        => await _context.EventosOutbox.OrderByDescending(e => e.FechaCreacion).ToListAsync(ct);

    public void Actualizar(EventoOutbox evento)
        => _context.EventosOutbox.Update(evento);
}
