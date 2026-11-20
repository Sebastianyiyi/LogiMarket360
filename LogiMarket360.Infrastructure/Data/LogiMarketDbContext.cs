namespace LogiMarket360.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using LogiMarket360.Domain.Entities;
using LogiMarket360.Domain.Enums;

public class LogiMarketDbContext : DbContext
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Kit> Kits => Set<Kit>();
    public DbSet<KitComponente> KitComponentes => Set<KitComponente>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoDetalle> PedidoDetalles => Set<PedidoDetalle>();
    public DbSet<PedidoDetalleComponente> PedidoDetalleComponentes => Set<PedidoDetalleComponente>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<Despacho> Despachos => Set<Despacho>();
    public DbSet<Devolucion> Devoluciones => Set<Devolucion>();
    public DbSet<EventoOutbox> EventosOutbox => Set<EventoOutbox>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();

    public LogiMarketDbContext(DbContextOptions<LogiMarketDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Usuario
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Username).HasMaxLength(50).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.PasswordSalt).IsRequired();
            entity.Property(u => u.Rol).HasConversion<string>();
        });

        // Cliente
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.Identificacion).IsUnique();
            entity.Property(c => c.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(c => c.Identificacion).HasMaxLength(20).IsRequired();
            entity.Property(c => c.Categoria).HasConversion<string>();
        });

        // Producto
        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.Property(p => p.Sku).HasMaxLength(20).IsRequired();
            entity.Property(p => p.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Precio).HasColumnType("TEXT");
            entity.Property(p => p.PesoKg).HasColumnType("TEXT");
            entity.Property(p => p.ConcurrencyToken).IsConcurrencyToken();
        });

        // Kit y KitComponente
        modelBuilder.Entity<Kit>(entity =>
        {
            entity.HasKey(k => k.Id);
            entity.HasIndex(k => k.CodigoKit).IsUnique();
            entity.Property(k => k.CodigoKit).HasMaxLength(30).IsRequired();
            entity.Property(k => k.Nombre).HasMaxLength(100).IsRequired();
            entity.HasMany(k => k.Componentes)
                  .WithOne()
                  .HasForeignKey(c => c.KitPadreId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KitComponente>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.TipoElemento).HasMaxLength(20).IsRequired();
            entity.Property(c => c.ElementoCodigo).HasMaxLength(30).IsRequired();
        });

        // Pedido
        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.NumeroPedido).IsUnique();
            entity.HasIndex(p => p.IdempotencyKey).IsUnique();
            entity.Property(p => p.NumeroPedido).HasMaxLength(50).IsRequired();
            entity.Property(p => p.IdempotencyKey).HasMaxLength(100).IsRequired();
            entity.Property(p => p.EstadoCodigo).HasConversion<string>();
            entity.Property(p => p.TipoEntrega).HasConversion<string>();
            entity.Property(p => p.Subtotal).HasColumnType("TEXT");
            entity.Property(p => p.Descuento).HasColumnType("TEXT");
            entity.Property(p => p.CostoEnvio).HasColumnType("TEXT");
            entity.Property(p => p.Total).HasColumnType("TEXT");

            entity.Ignore(p => p.Estado); // State Pattern se maneja en el agregado y se sincroniza con EstadoCodigo

            entity.HasMany(p => p.Detalles)
                  .WithOne()
                  .HasForeignKey(d => d.PedidoId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PedidoDetalle
        modelBuilder.Entity<PedidoDetalle>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.CodigoItem).HasMaxLength(30).IsRequired();
            entity.Property(d => d.NombreItem).HasMaxLength(100).IsRequired();
            entity.Property(d => d.PrecioUnitarioSnapshot).HasColumnType("TEXT");
            entity.Property(d => d.SubtotalLinea).HasColumnType("TEXT");

            entity.HasMany(d => d.ComponentesSnapshot)
                  .WithOne()
                  .HasForeignKey(c => c.PedidoDetalleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PedidoDetalleComponente
        modelBuilder.Entity<PedidoDetalleComponente>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Sku).HasMaxLength(20).IsRequired();
            entity.Property(c => c.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(c => c.PrecioUnitarioSnapshot).HasColumnType("TEXT");
            entity.Property(c => c.PesoKgSnapshot).HasColumnType("TEXT");
        });

        // Pago
        modelBuilder.Entity<Pago>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.ReferenciaTransaccion).IsUnique();
            entity.HasIndex(p => p.ClaveIdempotencia).IsUnique();
            entity.Property(p => p.Monto).HasColumnType("TEXT");
            entity.Property(p => p.ReferenciaTransaccion).HasMaxLength(50).IsRequired();
            entity.Property(p => p.ClaveIdempotencia).HasMaxLength(100).IsRequired();
            entity.Property(p => p.MetodoPago).HasMaxLength(50);
            entity.Property(p => p.ProveedorSimulado).HasMaxLength(50);
        });

        // MovimientoInventario
        modelBuilder.Entity<MovimientoInventario>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Sku).HasMaxLength(20).IsRequired();
            entity.Property(m => m.TipoMovimiento).HasConversion<string>();
            entity.Property(m => m.Motivo).HasMaxLength(200);
            entity.Property(m => m.Referencia).HasMaxLength(100);
        });

        // Despacho
        modelBuilder.Entity<Despacho>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.HasIndex(d => d.NumeroGuia).IsUnique();
            entity.Property(d => d.NumeroGuia).HasMaxLength(50).IsRequired();
            entity.Property(d => d.ProveedorLogistico).HasMaxLength(100);
            entity.Property(d => d.TipoEntrega).HasConversion<string>();
        });

        // Devolucion
        modelBuilder.Entity<Devolucion>(entity =>
        {
            entity.HasKey(d => d.Id);
            // Restricción de devolución única por pedido
            entity.HasIndex(d => d.PedidoId).IsUnique();
            entity.Property(d => d.Motivo).HasConversion<string>();
            entity.Property(d => d.MontoReembolso).HasColumnType("TEXT");
            entity.Property(d => d.ReferenciaReembolso).HasMaxLength(50);
        });

        // EventoOutbox
        modelBuilder.Entity<EventoOutbox>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TipoEvento).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ContenidoJson).IsRequired();
            entity.HasIndex(e => e.Procesado);
        });

        // Auditoria
        modelBuilder.Entity<Auditoria>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Usuario).HasMaxLength(50).IsRequired();
            entity.Property(a => a.Operacion).HasMaxLength(100).IsRequired();
            entity.Property(a => a.Entidad).HasMaxLength(50).IsRequired();
        });
    }
}
