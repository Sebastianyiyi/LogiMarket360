using LogiMarket360.Application.DTOs;
using LogiMarket360.Application.Facade;
using LogiMarket360.Application.Patterns.Command;
using LogiMarket360.Application.Services;
using LogiMarket360.Domain.Enums;
using LogiMarket360.Infrastructure.Data;
using LogiMarket360.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket360.Tests;

public sealed class OfficialFunctionalCasesTests
{
    [Fact(DisplayName = "P01 - login y permisos")]
    public async Task P01_LoginValidoEInvalido()
    {
        await using var app = await TestApplication.CreateAsync();
        var auth = app.Get<AutenticacionServicio>();
        Assert.False((await auth.IniciarSesionAsync("admin", "mala")).Exitoso);
        Assert.True((await auth.IniciarSesionAsync("admin", "admin123")).Exitoso);
        auth.CerrarSesion();
        Assert.Null(auth.ObtenerSesionActual());
    }

    [Fact(DisplayName = "P02 - catálogo y kits oficiales")]
    public async Task P02_CatalogoOficial()
    {
        await using var app = await TestApplication.CreateAsync(); await app.LoginAsync("admin", "admin123");
        var catalogo = app.Get<CatalogoServicio>();
        Assert.Equal(5, (await catalogo.ListarProductosAsync()).Count);
        var kits = await catalogo.ListarKitsAsync();
        Assert.Equal(50m, kits.Single(k => k.Codigo == "KIT_OFICINA").Precio);
        Assert.Equal(60m, kits.Single(k => k.Codigo == "KIT_TELETRABAJO").Precio);
    }

    [Fact(DisplayName = "P03 - Ana VIP kit teletrabajo")]
    public async Task P03_AnaVip_Total59YStockCorrecto()
    {
        await using var app = await TestApplication.CreateAsync(); await app.LoginAsync("ventas", "ventas123");
        var pedido = await CrearAsync(app, 1, TipoEntrega.ESTANDAR, new LineaPedidoSolicitud("KIT_TELETRABAJO", 1, true));
        Assert.Equal(59m, pedido.Total);
        var confirmado = await app.Get<ConfirmacionPedidoFacade>().ConfirmarAsync(pedido.Id, pedido.IdempotencyKey);
        Assert.True(confirmado.Exitoso, confirmado.Mensaje);
        var db = app.Get<LogiMarketDbContext>();
        Assert.Equal(7, (await db.Productos.SingleAsync(p => p.Sku == "P01")).Stock);
        Assert.Equal(4, (await db.Productos.SingleAsync(p => p.Sku == "P02")).Stock);
        Assert.Equal(19, (await db.Productos.SingleAsync(p => p.Sku == "P03")).Stock);
    }

    [Fact(DisplayName = "P04 - Luis normal exprés")]
    public async Task P04_Luis_Total102()
    {
        await using var app = await TestApplication.CreateAsync(); await app.LoginAsync("ventas", "ventas123");
        var pedido = await CrearAsync(app, 2, TipoEntrega.EXPRES, new LineaPedidoSolicitud("P04", 2, false), new LineaPedidoSolicitud("P05", 2, false));
        Assert.Equal(102m, pedido.Total);
        Assert.True((await app.Get<ConfirmacionPedidoFacade>().ConfirmarAsync(pedido.Id, pedido.IdempotencyKey)).Exitoso);
    }

    [Fact(DisplayName = "P05 - Carmen mayorista")]
    public async Task P05_Carmen_Total90()
    {
        await using var app = await TestApplication.CreateAsync(); await app.LoginAsync("ventas", "ventas123");
        var pedido = await CrearAsync(app, 3, TipoEntrega.ESTANDAR, new LineaPedidoSolicitud("P03", 10, false));
        Assert.Equal(90m, pedido.Total);
        Assert.Equal("MAYORISTA", pedido.EstrategiaDescuento);
    }

    [Fact(DisplayName = "P06 - stock insuficiente no altera datos")]
    public async Task P06_KitSeisVeces_RechazadoSinCambios()
    {
        await using var app = await TestApplication.CreateAsync(); await app.LoginAsync("ventas", "ventas123");
        var pedido = await CrearAsync(app, 1, TipoEntrega.ESTANDAR, new LineaPedidoSolicitud("KIT_TELETRABAJO", 6, true));
        var result = await app.Get<ConfirmacionPedidoFacade>().ConfirmarAsync(pedido.Id, pedido.IdempotencyKey);
        Assert.False(result.Exitoso);
        Assert.Equal(5, (await app.Get<LogiMarketDbContext>().Productos.SingleAsync(p => p.Sku == "P02")).Stock);
        Assert.Empty(app.Get<LogiMarketDbContext>().Pagos);
    }

    [Fact(DisplayName = "P07 - idempotencia secuencial")]
    public async Task P07_DobleConfirmacion_UnPagoUnDescuento()
    {
        await using var app = await TestApplication.CreateAsync(); await app.LoginAsync("ventas", "ventas123");
        var pedido = await CrearAsync(app, 1, TipoEntrega.ESTANDAR, new LineaPedidoSolicitud("P01", 1, false));
        var facade = app.Get<ConfirmacionPedidoFacade>();
        Assert.True((await facade.ConfirmarAsync(pedido.Id, pedido.IdempotencyKey)).Exitoso);
        var segundo = await facade.ConfirmarAsync(pedido.Id, pedido.IdempotencyKey);
        Assert.True(segundo.EsIdempotente);
        Assert.Single(app.Get<LogiMarketDbContext>().Pagos);
        Assert.Equal(7, (await app.Get<LogiMarketDbContext>().Productos.SingleAsync(p => p.Sku == "P01")).Stock);
    }

    [Fact(DisplayName = "P08 - pedido pagado inmutable")]
    public async Task P08_PagadoNoPuedeCancelarse()
    {
        await using var app = await TestApplication.CreateAsync(); await app.LoginAsync("ventas", "ventas123");
        var pedido = await CrearAsync(app, 1, TipoEntrega.ESTANDAR, new LineaPedidoSolicitud("P01", 1, false));
        await app.Get<ConfirmacionPedidoFacade>().ConfirmarAsync(pedido.Id, pedido.IdempotencyKey);
        var result = await app.Get<ICommandInvoker>().InvocarAsync(app.Get<ICommandFactory>().CrearCancelar(pedido.Id));
        Assert.False(result.Exitoso);
    }

    [Fact(DisplayName = "P09 - despacho y entrega")]
    public async Task P09_SecuenciaLogisticaValida()
    {
        await using var app = await TestApplication.CreateAsync(); await app.LoginAsync("ventas", "ventas123");
        var pedido = await CrearAsync(app, 1, TipoEntrega.ESTANDAR, new LineaPedidoSolicitud("P01", 1, false));
        await app.Get<ConfirmacionPedidoFacade>().ConfirmarAsync(pedido.Id, pedido.IdempotencyKey);
        app.Get<AutenticacionServicio>().CerrarSesion(); await app.LoginAsync("bodega", "bodega123");
        var invoker = app.Get<ICommandInvoker>(); var factory = app.Get<ICommandFactory>();
        Assert.True((await invoker.InvocarAsync(factory.CrearDespachar(pedido.Id, "GUIA-P09", "TEST"))).Exitoso);
        Assert.True((await invoker.InvocarAsync(factory.CrearEntregar(pedido.Id))).Exitoso);
    }

    [Fact(DisplayName = "P10 - persistencia tras reinicio")]
    public async Task P10_ReaperturaConservaCatalogo()
    {
        string path = Path.Combine(Path.GetTempPath(), $"lm360-p10-{Guid.NewGuid():N}.db");
        await using (var first = await TestApplication.CreateAsync(path, false))
        {
            await first.LoginAsync("ventas", "ventas123");
            var pedido = await CrearAsync(first, 1, TipoEntrega.ESTANDAR, new LineaPedidoSolicitud("P01", 1, false));
            await first.Get<ConfirmacionPedidoFacade>().ConfirmarAsync(pedido.Id, pedido.IdempotencyKey);
        }
        await using (var reopened = await TestApplication.CreateAsync(path, false))
        {
            Assert.Single(reopened.Get<LogiMarketDbContext>().Pagos);
            Assert.Equal(7, (await reopened.Get<LogiMarketDbContext>().Productos.SingleAsync(p => p.Sku == "P01")).Stock);
        }
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(path);
    }

    private static async Task<PedidoResumenDto> CrearAsync(TestApplication app, int clienteId, TipoEntrega entrega, params LineaPedidoSolicitud[] lineas)
    {
        var servicio = app.Get<PedidoBorradorServicio>();
        var result = await servicio.CrearBorradorAsync(clienteId, entrega, lineas);
        Assert.True(result.Exitoso, result.Mensaje);
        return (await servicio.ObtenerAsync(result.EntidadId!.Value))!;
    }
}
