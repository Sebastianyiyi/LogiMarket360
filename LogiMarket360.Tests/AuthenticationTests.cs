using LogiMarket360.Application.Services;
using LogiMarket360.Domain.Enums;
using LogiMarket360.Infrastructure.Data;
using LogiMarket360.Infrastructure.DI;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket360.Tests;

public sealed class AuthenticationTests
{
    [Theory]
    [InlineData("admin", "admin123", RolUsuario.ADMIN)]
    [InlineData("ventas", "ventas123", RolUsuario.VENTAS)]
    [InlineData("bodega", "bodega123", RolUsuario.BODEGA)]
    [InlineData("consulta", "consulta123", RolUsuario.CONSULTA)]
    public async Task UsuariosSeed_InicianSesionConSuRol(string username, string password, RolUsuario rol)
    {
        string database = Path.Combine(Path.GetTempPath(), $"lm360-auth-{Guid.NewGuid():N}.db");
        try
        {
            var services = new ServiceCollection();
            services.AddInfrastructure($"Data Source={database}");
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LogiMarketDbContext>();
            await DatabaseSeeder.InicializarAsync(db);

            var auth = scope.ServiceProvider.GetRequiredService<AutenticacionServicio>();
            var resultado = await auth.IniciarSesionAsync(username, password);

            Assert.True(resultado.Exitoso);
            Assert.Equal(rol, resultado.Usuario?.Rol);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(database)) File.Delete(database);
        }
    }

    [Fact]
    public async Task PasswordIncorrecta_NoCreaSesion()
    {
        string database = Path.Combine(Path.GetTempPath(), $"lm360-auth-{Guid.NewGuid():N}.db");
        try
        {
            var services = new ServiceCollection();
            services.AddInfrastructure($"Data Source={database}");
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LogiMarketDbContext>();
            await DatabaseSeeder.InicializarAsync(db);
            var auth = scope.ServiceProvider.GetRequiredService<AutenticacionServicio>();

            var resultado = await auth.IniciarSesionAsync("admin", "incorrecta");

            Assert.False(resultado.Exitoso);
            Assert.Null(auth.ObtenerSesionActual());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(database)) File.Delete(database);
        }
    }
}
