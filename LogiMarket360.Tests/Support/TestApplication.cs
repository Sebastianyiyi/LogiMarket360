using LogiMarket360.Application.DTOs;
using LogiMarket360.Application.Ports;
using LogiMarket360.Application.Services;
using LogiMarket360.Infrastructure.Data;
using LogiMarket360.Infrastructure.DI;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket360.Tests.Support;

internal sealed class TestApplication : IAsyncDisposable
{
    private readonly ServiceProvider _provider;
    private readonly AsyncServiceScope _scope;
    private readonly bool _ownsDatabase;

    private TestApplication(string databasePath, ServiceProvider provider, AsyncServiceScope scope, bool ownsDatabase)
    {
        DatabasePath = databasePath;
        _provider = provider;
        _scope = scope;
        _ownsDatabase = ownsDatabase;
    }

    public string DatabasePath { get; }
    public IServiceProvider Services => _scope.ServiceProvider;

    public static async Task<TestApplication> CreateAsync(
        string? databasePath = null,
        bool ownsDatabase = true,
        Action<IServiceCollection>? configure = null)
    {
        databasePath ??= Path.Combine(Path.GetTempPath(), $"lm360-{Guid.NewGuid():N}.db");
        var services = new ServiceCollection();
        services.AddInfrastructure($"Data Source={databasePath}");
        services.AddScoped<IPasarelaPago, PasarelaDeterminista>();
        configure?.Invoke(services);
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var scope = provider.CreateAsyncScope();
        await DatabaseSeeder.InicializarAsync(scope.ServiceProvider.GetRequiredService<LogiMarketDbContext>());
        return new TestApplication(databasePath, provider, scope, ownsDatabase);
    }

    public T Get<T>() where T : notnull => Services.GetRequiredService<T>();

    public async Task LoginAsync(string usuario, string password)
    {
        var result = await Get<AutenticacionServicio>().IniciarSesionAsync(usuario, password);
        Assert.True(result.Exitoso, result.Mensaje);
    }

    public async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();
        await _provider.DisposeAsync();
        SqliteConnection.ClearAllPools();
        if (_ownsDatabase && File.Exists(DatabasePath)) File.Delete(DatabasePath);
    }

    private sealed class PasarelaDeterminista : IPasarelaPago
    {
        public string NombreProveedor => "DETERMINISTA";
        public Task<ResultadoPago> CobrarAsync(decimal monto, string claveIdempotencia, CancellationToken ct = default) =>
            Task.FromResult(new ResultadoPago(true, $"DET-{claveIdempotencia}", "Aprobado", monto, NombreProveedor));
    }
}
