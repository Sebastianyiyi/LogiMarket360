using LogiMarket360.Application.Ports;
using LogiMarket360.Application.Security;
using LogiMarket360.Domain.Enums;
using LogiMarket360.Infrastructure.Adapters;
using LogiMarket360.Infrastructure.DI;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket360.Tests;

public sealed class PaymentProviderSelectionTests
{
    [Fact]
    public async Task Proveedores_Normalmente_ApruebanDeFormaDeterminista()
    {
        IPasarelaPago[] proveedores =
        {
            new PasarelaLegadaAdapter(new PasarelaLegadaSimulada()),
            new PasarelaModernaSimulada()
        };

        foreach (var proveedor in proveedores)
        {
            for (int intento = 0; intento < 250; intento++)
            {
                var resultado = await proveedor.CobrarAsync(59m, $"{proveedor.NombreProveedor}-{intento}");
                Assert.True(resultado.Exitoso, $"{proveedor.NombreProveedor} rechazó el intento {intento} en modo normal.");
                Assert.Equal(59m, resultado.MontoCobrado);
                Assert.Equal(proveedor.NombreProveedor, resultado.Proveedor);
            }
        }
    }

    [Fact]
    public async Task Selector_CambiaProveedorYFallaSoloCuandoSeActivaElModoControlado()
    {
        var selector = new ProveedorPagoSelector(
            new PasarelaLegadaAdapter(new PasarelaLegadaSimulada()),
            new PasarelaModernaSimulada());

        Assert.Equal(new[] { "LEGADA", "MODERNA" }, selector.ProveedoresDisponibles.OrderBy(x => x));
        Assert.Equal("LEGADA", selector.ProveedorActivo);
        Assert.True((await selector.CobrarAsync(59m, "LEG-1")).Exitoso);

        selector.Seleccionar("moderna");
        var moderna = await selector.CobrarAsync(59m, "MOD-1");
        Assert.True(moderna.Exitoso);
        Assert.Equal("MODERNA", moderna.Proveedor);

        selector.ConfigurarModoFallo(true);
        var rechazado = await selector.CobrarAsync(59m, "MOD-FALLO");
        Assert.False(rechazado.Exitoso);
        Assert.Equal(0m, rechazado.MontoCobrado);
        Assert.Equal("MODERNA", rechazado.Proveedor);

        selector.ConfigurarModoFallo(false);
        Assert.True((await selector.CobrarAsync(59m, "MOD-2")).Exitoso);
    }

    [Fact]
    public async Task CadenaRegistrada_RespetaSeleccionEnTiempoDeEjecucion()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure("Data Source=:memory:");
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        await using var scope = provider.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<SesionServicio>()
            .Iniciar(new UsuarioSesion(1, "ventas", RolUsuario.VENTAS));

        var selector = scope.ServiceProvider.GetRequiredService<IProveedorPagoSelector>();
        var cadena = scope.ServiceProvider.GetRequiredService<IPasarelaPago>();

        selector.Seleccionar("LEGADA");
        Assert.Equal("LEGADA", (await cadena.CobrarAsync(59m, "CHAIN-LEG")).Proveedor);

        selector.Seleccionar("MODERNA");
        Assert.Equal("MODERNA", (await cadena.CobrarAsync(59m, "CHAIN-MOD")).Proveedor);

        selector.ConfigurarModoFallo(true);
        Assert.False((await cadena.CobrarAsync(59m, "CHAIN-FAIL")).Exitoso);
    }
}
