using LogiMarket360.Application.DTOs;
using LogiMarket360.Application.Facade;
using LogiMarket360.Application.Patterns.Command;
using LogiMarket360.Application.Patterns.TemplateMethod;
using LogiMarket360.Application.Ports;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Application.Services;
using LogiMarket360.Domain.Entities;
using LogiMarket360.Domain.Enums;
using LogiMarket360.Infrastructure.Data;
using LogiMarket360.Tests.Support;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket360.Tests;

public sealed class IntegrityRecoveryTests
{
    [Fact(DisplayName = "P06 - cobro rechazado conserva estado e inventario en contexto nuevo")]
    public async Task CobroRechazado_ContextoNuevo_ConservaBorradorSinEfectos()
    {
        string path = Path.Combine(Path.GetTempPath(), $"lm360-p06-{Guid.NewGuid():N}.db");
        try
        {
            await using var app = await TestApplication.CreateAsync(
                path,
                false,
                services => services.AddScoped<IPasarelaPago, LogiMarket360.Infrastructure.Adapters.PasarelaFallaSimulada>());
            await app.LoginAsync("ventas", "ventas123");
            var pedido = await CrearAsync(app, 1, new LineaPedidoSolicitud("P01", 1, false));

            var resultado = await app.Get<ConfirmacionPedidoFacade>()
                .ConfirmarAsync(pedido.Id, pedido.IdempotencyKey);

            Assert.False(resultado.Exitoso);
            Assert.Empty(app.Get<LogiMarketDbContext>().ChangeTracker.Entries());
            await using var db = CrearContexto(path);
            Assert.Equal(EstadoPedido.BORRADOR, (await db.Pedidos.SingleAsync()).EstadoCodigo);
            Assert.Equal(8, (await db.Productos.SingleAsync(p => p.Sku == "P01")).Stock);
            Assert.Empty(await db.Pagos.ToListAsync());
            Assert.Empty(await db.EventosOutbox.ToListAsync());
            Assert.Empty(await db.MovimientosInventario.ToListAsync());
        }
        finally { SqliteConnection.ClearAllPools(); if (File.Exists(path)) File.Delete(path); }
    }

    [Fact(DisplayName = "P22 - autorización ocurre antes de cualquier colaborador de cobro")]
    public async Task ConsultaIntentandoConfirmar_NoMutaNiInvocaPasarela()
    {
        string path = Path.Combine(Path.GetTempPath(), $"lm360-p22-{Guid.NewGuid():N}.db");
        var pasarela = new PasarelaContadora();
        try
        {
            await using var app = await TestApplication.CreateAsync(
                path,
                false,
                services => services.AddScoped<IPasarelaPago>(_ => pasarela));
            await app.LoginAsync("ventas", "ventas123");
            var pedido = await CrearAsync(app, 1, new LineaPedidoSolicitud("P01", 1, false));
            app.Get<AutenticacionServicio>().CerrarSesion();
            await app.LoginAsync("consulta", "consulta123");

            var resultado = await app.Get<ConfirmacionPedidoFacade>()
                .ConfirmarAsync(pedido.Id, pedido.IdempotencyKey);

            Assert.False(resultado.Exitoso);
            Assert.Equal(0, pasarela.Invocaciones);
            await using var db = CrearContexto(path);
            Assert.Equal(EstadoPedido.BORRADOR, (await db.Pedidos.SingleAsync()).EstadoCodigo);
            Assert.Equal(8, (await db.Productos.SingleAsync(p => p.Sku == "P01")).Stock);
            Assert.Empty(await db.Pagos.ToListAsync());
            Assert.Empty(await db.EventosOutbox.ToListAsync());
        }
        finally { SqliteConnection.ClearAllPools(); if (File.Exists(path)) File.Delete(path); }
    }

    [Fact(DisplayName = "P20 - excepción intermedia revierte todos los cambios y limpia tracking")]
    public async Task ExcepcionIntermedia_RollbackSinPersistenciaParcial()
    {
        string path = Path.Combine(Path.GetTempPath(), $"lm360-p20-{Guid.NewGuid():N}.db");
        try
        {
            await using var app = await TestApplication.CreateAsync(
                path,
                false,
                services => services.AddScoped<IEventoOutboxRepository, OutboxConFallo>());
            await app.LoginAsync("ventas", "ventas123");
            var pedido = await CrearAsync(app, 1, new LineaPedidoSolicitud("P01", 1, false));

            var resultado = await app.Get<ConfirmacionPedidoFacade>()
                .ConfirmarAsync(pedido.Id, pedido.IdempotencyKey);

            Assert.False(resultado.Exitoso);
            Assert.Empty(app.Get<LogiMarketDbContext>().ChangeTracker.Entries());
            await using var db = CrearContexto(path);
            Assert.Equal(EstadoPedido.BORRADOR, (await db.Pedidos.SingleAsync()).EstadoCodigo);
            Assert.Equal(8, (await db.Productos.SingleAsync(p => p.Sku == "P01")).Stock);
            Assert.Empty(await db.Pagos.ToListAsync());
            Assert.Empty(await db.EventosOutbox.ToListAsync());
            Assert.Empty(await db.MovimientosInventario.ToListAsync());
        }
        finally { SqliteConnection.ClearAllPools(); if (File.Exists(path)) File.Delete(path); }
    }

    [Fact(DisplayName = "P20 - Facade revierte ante cobro rechazado")]
    public async Task P20_CobroRechazado_NoPersisteStockPagoNiOutbox()
    {
        await using var app = await TestApplication.CreateAsync(configure: s => s.AddScoped<IPasarelaPago, LogiMarket360.Infrastructure.Adapters.PasarelaFallaSimulada>());
        await app.LoginAsync("ventas", "ventas123");
        var pedido = await CrearAsync(app, 1, new LineaPedidoSolicitud("P01", 1, false));
        Assert.False((await app.Get<ConfirmacionPedidoFacade>().ConfirmarAsync(pedido.Id, pedido.IdempotencyKey)).Exitoso);
        var db = app.Get<LogiMarketDbContext>();
        Assert.Equal(8, (await db.Productos.SingleAsync(p => p.Sku == "P01")).Stock);
        Assert.Empty(db.Pagos); Assert.Empty(db.EventosOutbox);
    }

    [Fact(DisplayName = "P07b - idempotencia concurrente")]
    public async Task IdempotenciaConcurrente_UnPagoYUnMovimiento()
    {
        string path = Path.Combine(Path.GetTempPath(), $"lm360-idem-{Guid.NewGuid():N}.db");
        try
        {
            await using var app1 = await TestApplication.CreateAsync(path, false); await app1.LoginAsync("ventas", "ventas123");
            var pedido = await CrearAsync(app1, 1, new LineaPedidoSolicitud("P01", 1, false));
            await using var app2 = await TestApplication.CreateAsync(path, false); await app2.LoginAsync("ventas", "ventas123");
            await Task.WhenAll(
                app1.Get<ConfirmacionPedidoFacade>().ConfirmarAsync(pedido.Id, pedido.IdempotencyKey),
                app2.Get<ConfirmacionPedidoFacade>().ConfirmarAsync(pedido.Id, pedido.IdempotencyKey));
            app1.Get<LogiMarketDbContext>().ChangeTracker.Clear();
            Assert.Single(await app1.Get<LogiMarketDbContext>().Pagos.ToListAsync());
            Assert.Equal(7, (await app1.Get<LogiMarketDbContext>().Productos.SingleAsync(p => p.Sku == "P01")).Stock);
            Assert.Single(await app1.Get<LogiMarketDbContext>().MovimientosInventario.Where(m => m.Sku == "P01" && m.Cantidad == -1).ToListAsync());
        }
        finally { SqliteConnection.ClearAllPools(); if (File.Exists(path)) File.Delete(path); }
    }

    [Fact(DisplayName = "P35 - devolución única y reintegro único")]
    public async Task P35_DevolucionAna_Reembolsa54YNoDuplicaStock()
    {
        await using var app = await TestApplication.CreateAsync();
        await app.LoginAsync("ventas", "ventas123");
        var pedido = await CrearAsync(app, 1, new LineaPedidoSolicitud("KIT_TELETRABAJO", 1, true));
        await app.Get<ConfirmacionPedidoFacade>().ConfirmarAsync(pedido.Id, pedido.IdempotencyKey);
        app.Get<AutenticacionServicio>().CerrarSesion(); await app.LoginAsync("bodega", "bodega123");
        var invoker = app.Get<ICommandInvoker>(); var factory = app.Get<ICommandFactory>();
        Assert.True((await invoker.InvocarAsync(factory.CrearDespachar(pedido.Id, "GUIA-35", "TEST"))).Exitoso);
        Assert.True((await invoker.InvocarAsync(factory.CrearEntregar(pedido.Id))).Exitoso);
        app.Get<AutenticacionServicio>().CerrarSesion(); await app.LoginAsync("ventas", "ventas123");
        var solicitud = new SolicitudDevolucion(pedido.Id, MotivoDevolucion.DEFECTO, 20, false, "Defecto comprobado");
        Assert.True((await invoker.InvocarAsync(factory.CrearDevolucion(solicitud))).Exitoso);
        Assert.False((await invoker.InvocarAsync(factory.CrearDevolucion(solicitud))).Exitoso);
        var db = app.Get<LogiMarketDbContext>();
        Assert.Equal(54m, (await db.Devoluciones.SingleAsync()).MontoReembolso);
        Assert.Equal(8, (await db.Productos.SingleAsync(p => p.Sku == "P01")).Stock);
        Assert.Single(db.Devoluciones);
    }

    [Fact(DisplayName = "P33 - Template Method aplica plazos")]
    public async Task P33_ArrepentimientoDia10_RechazaSinReintegro()
    {
        await using var app = await TestApplication.CreateAsync(); await app.LoginAsync("ventas", "ventas123");
        var pedido = await CrearAsync(app, 1, new LineaPedidoSolicitud("P01", 1, false));
        await app.Get<ConfirmacionPedidoFacade>().ConfirmarAsync(pedido.Id, pedido.IdempotencyKey);
        app.Get<AutenticacionServicio>().CerrarSesion(); await app.LoginAsync("bodega", "bodega123");
        var invoker = app.Get<ICommandInvoker>(); var factory = app.Get<ICommandFactory>();
        await invoker.InvocarAsync(factory.CrearDespachar(pedido.Id, "GUIA-33", "TEST"));
        await invoker.InvocarAsync(factory.CrearEntregar(pedido.Id));
        app.Get<AutenticacionServicio>().CerrarSesion(); await app.LoginAsync("ventas", "ventas123");
        var r = await invoker.InvocarAsync(factory.CrearDevolucion(new SolicitudDevolucion(
            pedido.Id, MotivoDevolucion.ARREPENTIMIENTO, 10, true, "Fuera de plazo")));
        Assert.False(r.Exitoso);
        Assert.Equal(7, (await app.Get<LogiMarketDbContext>().Productos.SingleAsync(p => p.Sku == "P01")).Stock);
        var devolucion = await app.Get<LogiMarketDbContext>().Devoluciones.SingleAsync();
        Assert.False(devolucion.Aprobada);
        Assert.Equal(0m, devolucion.MontoReembolso);
    }

    [Fact(DisplayName = "P36 - concurrencia impide sobreventa")]
    public async Task P36_DosPedidosConUnaUnidad_MaximoUnoConfirma()
    {
        string path = Path.Combine(Path.GetTempPath(), $"lm360-p36-{Guid.NewGuid():N}.db");
        try
        {
            await using var app1 = await TestApplication.CreateAsync(path, false);
            await app1.LoginAsync("admin", "admin123");
            var producto = await app1.Get<LogiMarketDbContext>().Productos.SingleAsync(p => p.Sku == "P04");
            await app1.Get<CatalogoServicio>().AjustarStockAsync(producto.Id, 1, "P36", "P36");
            app1.Get<AutenticacionServicio>().CerrarSesion(); await app1.LoginAsync("ventas", "ventas123");
            var p1 = await CrearAsync(app1, 1, new LineaPedidoSolicitud("P04", 1, false));
            var p2 = await CrearAsync(app1, 2, new LineaPedidoSolicitud("P04", 1, false));
            await using var app2 = await TestApplication.CreateAsync(path, false); await app2.LoginAsync("ventas", "ventas123");
            var resultados = await Task.WhenAll(
                app1.Get<ConfirmacionPedidoFacade>().ConfirmarAsync(p1.Id, p1.IdempotencyKey),
                app2.Get<ConfirmacionPedidoFacade>().ConfirmarAsync(p2.Id, p2.IdempotencyKey));
            Assert.Equal(1, resultados.Count(r => r.Exitoso));
            app1.Get<LogiMarketDbContext>().ChangeTracker.Clear();
            Assert.Equal(0, (await app1.Get<LogiMarketDbContext>().Productos.SingleAsync(p => p.Sku == "P04")).Stock);
            Assert.Single(await app1.Get<LogiMarketDbContext>().Pagos.ToListAsync());
        }
        finally { SqliteConnection.ClearAllPools(); if (File.Exists(path)) File.Delete(path); }
    }

    [Fact(DisplayName = "P37 - Outbox se recupera tras reinicio")]
    public async Task P37_NotificadorFalla_ReinicioProcesaEventoPendiente()
    {
        string path = Path.Combine(Path.GetTempPath(), $"lm360-p37-{Guid.NewGuid():N}.db");
        try
        {
            await using (var failing = await TestApplication.CreateAsync(path, false, s => s.AddSingleton<INotificadorFactory, FailingFactory>()))
            {
                await failing.LoginAsync("ventas", "ventas123");
                var pedido = await CrearAsync(failing, 1, new LineaPedidoSolicitud("P01", 1, false));
                Assert.True((await failing.Get<ConfirmacionPedidoFacade>().ConfirmarAsync(pedido.Id, pedido.IdempotencyKey)).Exitoso);
                var evento = await failing.Get<LogiMarketDbContext>().EventosOutbox.SingleAsync();
                Assert.False(evento.Procesado); Assert.Equal(1, evento.Intentos);
            }
            await using (var recovered = await TestApplication.CreateAsync(path, false))
            {
                Assert.Equal(1, await recovered.Get<OutboxProcessor>().ProcesarPendientesAsync());
                var evento = await recovered.Get<LogiMarketDbContext>().EventosOutbox.SingleAsync();
                Assert.True(evento.Procesado);
                Assert.Single(recovered.Get<LogiMarketDbContext>().Pagos);
            }
        }
        finally { SqliteConnection.ClearAllPools(); if (File.Exists(path)) File.Delete(path); }
    }

    private static async Task<PedidoResumenDto> CrearAsync(TestApplication app, int clienteId, params LineaPedidoSolicitud[] lineas)
    {
        var servicio = app.Get<PedidoBorradorServicio>();
        var r = await servicio.CrearBorradorAsync(clienteId, TipoEntrega.ESTANDAR, lineas);
        Assert.True(r.Exitoso, r.Mensaje);
        return (await servicio.ObtenerAsync(r.EntidadId!.Value))!;
    }

    private static LogiMarketDbContext CrearContexto(string path)
    {
        var options = new DbContextOptionsBuilder<LogiMarketDbContext>()
            .UseSqlite($"Data Source={path}")
            .Options;
        return new LogiMarketDbContext(options);
    }

    private sealed class PasarelaContadora : IPasarelaPago
    {
        public int Invocaciones { get; private set; }
        public string NombreProveedor => "CONTADORA";

        public Task<ResultadoPago> CobrarAsync(decimal monto, string claveIdempotencia, CancellationToken ct = default)
        {
            Invocaciones++;
            return Task.FromResult(new ResultadoPago(true, "CONTADA", "Aprobado", monto, NombreProveedor));
        }
    }

    private sealed class OutboxConFallo : IEventoOutboxRepository
    {
        public Task AgregarAsync(EventoOutbox evento, CancellationToken ct = default) =>
            throw new InvalidOperationException("Fallo inducido después de modificar pedido, stock y pago.");

        public Task<IReadOnlyList<EventoOutbox>> ListarPendientesAsync(int limite = 50, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<EventoOutbox>>(Array.Empty<EventoOutbox>());

        public Task<IReadOnlyList<EventoOutbox>> ListarTodosAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<EventoOutbox>>(Array.Empty<EventoOutbox>());

        public void Actualizar(EventoOutbox evento) { }
    }

    private sealed class FailingFactory : INotificadorFactory
    {
        public string CanalPorDefecto => "FAIL";
        public INotificador Crear(string canal) => new FailingNotifier();
    }

    private sealed class FailingNotifier : INotificador
    {
        public string Canal => "FAIL";
        public Task<bool> EnviarAsync(string destinatario, string mensaje, CancellationToken ct = default) => Task.FromResult(false);
    }
}
