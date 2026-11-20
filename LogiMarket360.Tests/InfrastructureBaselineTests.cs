using LogiMarket360.Application.Facade;
using LogiMarket360.Application.Patterns.Command;
using LogiMarket360.Application.Patterns.Mediator;
using LogiMarket360.Application.Services;
using LogiMarket360.Domain.Entities;
using LogiMarket360.Infrastructure.Data;
using LogiMarket360.Infrastructure.DI;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket360.Tests;

public sealed class InfrastructureBaselineTests
{
    [Fact]
    public async Task MigracionYSeed_CreanCatalogoKitsClientesYUsuariosOficiales()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<LogiMarketDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new LogiMarketDbContext(options);
        await DatabaseSeeder.InicializarAsync(context);

        Assert.Equal(5, await context.Productos.CountAsync());
        Assert.Equal(2, await context.Kits.CountAsync());
        Assert.Equal(3, await context.Clientes.CountAsync());
        Assert.Equal(4, await context.Usuarios.CountAsync());
        Assert.Contains(await context.Productos.ToListAsync(), p => p.Sku == "P01" && p.Stock == 8);
        Assert.Contains(await context.Kits.ToListAsync(), k => k.CodigoKit == "KIT_TELETRABAJO");
    }

    [Fact]
    public async Task Seed_EsIdempotente()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<LogiMarketDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new LogiMarketDbContext(options);
        await DatabaseSeeder.InicializarAsync(context);
        await DatabaseSeeder.InicializarAsync(context);

        Assert.Equal(5, await context.Productos.CountAsync());
        Assert.Equal(2, await context.Kits.CountAsync());
        Assert.Equal(4, await context.Usuarios.CountAsync());
    }

    [Fact]
    public async Task BaseDeDatos_RechazaSkuDuplicado()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<LogiMarketDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new LogiMarketDbContext(options);
        await DatabaseSeeder.InicializarAsync(context);
        context.Productos.Add(new Producto("P01", "Duplicado", 1m, 1, 0m));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public void ContenedorDi_ResuelveServiciosPrincipales()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure("Data Source=:memory:");
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ConfirmacionPedidoFacade>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AutenticacionServicio>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<CatalogoServicio>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<OutboxProcessor>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICommandFactory>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICommandInvoker>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IFormularioPrincipalMediator>());
    }
}
