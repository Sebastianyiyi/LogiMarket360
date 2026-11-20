using LogiMarket360.Application.Documents;
using LogiMarket360.Application.DTOs;
using LogiMarket360.Application.Patterns.Builder;
using LogiMarket360.Application.Patterns.ChainOfResponsibility;
using LogiMarket360.Application.Patterns.Memento;
using LogiMarket360.Application.Patterns.Interpreter;
using LogiMarket360.Application.Patterns.Observer;
using LogiMarket360.Application.Patterns.Observer.Eventos;
using LogiMarket360.Application.Patterns.Prototype;
using LogiMarket360.Application.Ports;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Domain.Entities;
using LogiMarket360.Domain.Enums;
using LogiMarket360.Domain.Exceptions;
using LogiMarket360.Domain.Patterns.Composite;
using LogiMarket360.Domain.Patterns.Iterator;
using LogiMarket360.Domain.Patterns.Visitor;
using LogiMarket360.Infrastructure.Adapters;
using LogiMarket360.Infrastructure.Documents;
using LogiMarket360.Infrastructure.Notifications;

namespace LogiMarket360.Tests;

public sealed class RemainingPatternAndSolidTests
{
    [Fact(DisplayName = "P11 - Factory Method EMAIL y SMS")]
    public async Task P11_FactoryMethod_CreaCanalesCorrectos()
    {
        Assert.Equal("EMAIL", new NotificadorEmailCreator().CrearNotificador().Canal);
        Assert.Equal("SMS", new NotificadorSmsCreator().CrearNotificador().Canal);
        Assert.True(await new NotificadorEmailCreator().NotificarAsync("ana", "Pedido pagado"));
    }

    [Fact(DisplayName = "P13 - Builder completo e incompleto")]
    public void P13_Builder_ValidaConstruccion()
    {
        Assert.Throws<DomainException>(() => new PedidoBuilder().Build());
        var pedido = PedidoBase();
        Assert.Single(pedido.Detalles);
        Assert.Equal(25m, pedido.Total);
    }

    [Fact(DisplayName = "P14 - Prototype profundo")]
    public void P14_Prototype_NoComparteColecciones()
    {
        var original = PedidoBase();
        var clon = new PedidoBorradorPrototype(original).Clonar();
        clon.LimpiarDetalles();
        Assert.Single(original.Detalles);
        Assert.Empty(clon.Detalles);
        Assert.NotEqual(original.IdempotencyKey, clon.IdempotencyKey);
    }

    [Fact(DisplayName = "P16 - Adapter y proveedor sustituible")]
    public async Task P16_Proveedores_CumplenMismoContrato()
    {
        IPasarelaPago legado = new PasarelaLegadaAdapter(new PasarelaLegadaSimulada());
        IPasarelaPago fallo = new PasarelaFallaSimulada();
        Assert.Equal("LEGADA", legado.NombreProveedor);
        Assert.Equal("FALLA_TEST", fallo.NombreProveedor);
        Assert.False((await fallo.CobrarAsync(10m, "p16")).Exitoso);
    }

    [Fact(DisplayName = "P17 - Bridge comprobantes y salidas")]
    public async Task P17_Bridge_CuatroCombinaciones()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"lm360-p17-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            var venta = new DatosComprobanteVenta("P1", "Ana", "VIP", 60, 6, 5, 59, "R1", DateTime.UtcNow);
            var devolucion = new DatosComprobanteDevolucion("P1", "Ana", 54, "DEFECTO", "R2", DateTime.UtcNow);
            foreach (var salida in new ISalidaDocumento[] { new SalidaVistaPreviaTxt(), new SalidaArchivoTxt(dir) })
            {
                Assert.NotEmpty(await new ComprobanteVenta(salida, venta).EmitirAsync(venta));
                Assert.NotEmpty(await new ComprobanteDevolucion(salida, devolucion).EmitirAsync(devolucion));
            }
            Assert.Equal(2, Directory.GetFiles(dir).Length);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact(DisplayName = "P23 - Chain detiene primer error")]
    public async Task P23_Chain_DetienePrimerError()
    {
        var ejecutados = new List<string>();
        var primero = new PasoFalso("cliente", false, ejecutados);
        primero.SetNext(new PasoFalso("stock", false, ejecutados));
        var result = await primero.ValidateAsync(PedidoBase());
        Assert.False(result.EsValido);
        Assert.Equal(new[] { "cliente" }, ejecutados);
    }

    [Fact(DisplayName = "P27 - Iterator determinista")]
    public void P27_Iterator_RecorreTresSkuYTermina()
    {
        var iterator = new ComponenteIterator(Teletrabajo()); var sku = new List<string>();
        while (iterator.HasNext()) sku.Add(iterator.Next().Sku);
        Assert.Equal(new[] { "P01", "P02", "P03" }, sku.OrderBy(x => x));
        Assert.Throws<InvalidOperationException>(() => iterator.Next());
    }

    [Fact(DisplayName = "P25 - Interpreter combina predicados")]
    public void P25_Interpreter_FiltraEstadoYTotal()
    {
        var pedido = PedidoBase();
        var expression = new PedidoQueryParser().Parse("ESTADO = BORRADOR AND TOTAL >= 20");
        Assert.True(expression.Interpretar(new ContextoPedido(pedido, "Ana")));
        Assert.False(new PedidoQueryParser().Parse("TOTAL > 100 OR CLIENTE = Luis").Interpretar(new ContextoPedido(pedido, "Ana")));
    }

    [Fact(DisplayName = "P29 - Memento deshacer rehacer y bloquear")]
    public void P29_Memento_RestauraYBloquea()
    {
        var history = new PedidoBorradorHistory();
        var uno = PedidoBase(); history.GuardarEstado(new PedidoBorradorMemento(uno));
        uno.AgregarDetalle(Detalle("P02", 30m)); history.GuardarEstado(new PedidoBorradorMemento(uno));
        Assert.Single(history.Deshacer().Restaurar().Detalles);
        Assert.Equal(2, history.Rehacer().Restaurar().Detalles.Count);
        history.BloquearYLimpiar();
        Assert.Throws<InvalidOperationException>(() => history.GuardarEstado(new PedidoBorradorMemento(uno)));
    }

    [Fact(DisplayName = "P30 - Observer publica una vez")]
    public async Task P30_Observer_NotificaSuscriptores()
    {
        var publisher = new EventoPublicador(); var a = new ContadorObserver(); var b = new ContadorObserver();
        publisher.Suscribir<PedidoPagadoEvent>(a); publisher.Suscribir<PedidoPagadoEvent>(b);
        await publisher.PublicarAsync(new PedidoPagadoEvent(1, "P1", 1, 59, "R", DateTime.UtcNow));
        Assert.Equal(1, a.Conteo); Assert.Equal(1, b.Conteo);
    }

    [Fact(DisplayName = "P31 - State secuencia y rechazo")]
    public void P31_State_ModelaTransiciones()
    {
        var pedido = PedidoBase(); pedido.Validar(); pedido.Pagar();
        Assert.Throws<TransicionEstadoInvalidaException>(() => pedido.Cancelar());
        pedido.Despachar(); pedido.Entregar();
        Assert.Equal(EstadoPedido.ENTREGADO, pedido.EstadoCodigo);
    }

    [Fact(DisplayName = "P34 - Visitor desglose y peso")]
    public void P34_Visitor_ObtieneTresSkuYPeso()
    {
        var kit = Teletrabajo(); var desglose = new DesgloseSkuVisitor(); var peso = new PesoLogisticoVisitor();
        kit.Aceptar(desglose); kit.Aceptar(peso);
        Assert.Equal(3, desglose.ObtenerDesglose().Count);
        Assert.Equal(1m, peso.PesoTotalKg);
    }

    [Fact(DisplayName = "P38 - LSP proveedores sustituibles")]
    public async Task P38_Lsp_DosImplementacionesRespetanContrato()
    {
        foreach (IPasarelaPago gateway in new IPasarelaPago[] { new PasarelaFallaSimulada(), new GatewayExitoso() })
        {
            var result = await gateway.CobrarAsync(12m, "lsp");
            Assert.Equal(gateway.NombreProveedor, result.Proveedor);
            Assert.True(result.MontoCobrado is 0m or 12m);
        }
    }

    [Fact(DisplayName = "P38 - ISP contratos pequeños")]
    public void P38_Isp_InterfacesNoExponenOperacionesNoAplicables()
    {
        Assert.Equal(2, typeof(IPasarelaPago).GetMethods().Length);
        Assert.True(typeof(IProductoRepository).GetMethods().Length < 10);
        Assert.DoesNotContain(typeof(IProductoRepository).GetMethods(), m => m.Name.Contains("Pago"));
    }

    [Fact(DisplayName = "P38 - DIP Application no referencia Infrastructure")]
    public void P38_Dip_ApplicationSoloReferenciaAbstracciones()
    {
        var referencias = typeof(PedidoBuilder).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToList();
        Assert.DoesNotContain("LogiMarket360.Infrastructure", referencias);
        Assert.Contains("LogiMarket360.Domain", referencias);
    }

    private static Pedido PedidoBase() => new PedidoBuilder().ParaCliente(1, CategoriaCliente.NORMAL)
        .ConEntrega(TipoEntrega.ESTANDAR, 5m).AgregarProducto(new ProductoSimple("P01", "Mouse", 20m, .2m), 1).Build();
    private static PedidoDetalle Detalle(string sku, decimal precio) => new(sku, sku, false, 1, precio,
        new[] { new PedidoDetalleComponente(sku, sku, 1, precio, .1m) });
    private static KitCompuesto Teletrabajo()
    {
        var oficina = new KitCompuesto("KIT_OFICINA", "Oficina");
        oficina.AgregarComponente(new ProductoSimple("P01", "Mouse", 20m, .2m));
        oficina.AgregarComponente(new ProductoSimple("P02", "Teclado", 30m, .7m));
        var kit = new KitCompuesto("KIT_TELETRABAJO", "Teletrabajo"); kit.AgregarComponente(oficina);
        kit.AgregarComponente(new ProductoSimple("P03", "Memoria", 10m, .1m)); return kit;
    }

    private sealed class PasoFalso : ValidationStepBase
    {
        private readonly string _nombre; private readonly bool _valido; private readonly List<string> _ejecutados;
        public PasoFalso(string nombre, bool valido, List<string> ejecutados) { _nombre = nombre; _valido = valido; _ejecutados = ejecutados; }
        protected override Task<ValidationResult> ExecuteStepAsync(Pedido pedido, CancellationToken ct)
        { _ejecutados.Add(_nombre); return Task.FromResult(_valido ? ValidationResult.Valido() : ValidationResult.Invalido(_nombre, "fallo")); }
    }
    private sealed class ContadorObserver : IEventoSubscriptor<PedidoPagadoEvent>
    { public int Conteo { get; private set; } public Task ManejarAsync(PedidoPagadoEvent evento, CancellationToken ct = default) { Conteo++; return Task.CompletedTask; } }
    private sealed class GatewayExitoso : IPasarelaPago
    { public string NombreProveedor => "OK"; public Task<ResultadoPago> CobrarAsync(decimal monto, string claveIdempotencia, CancellationToken ct = default) => Task.FromResult(new ResultadoPago(true, "R", "OK", monto, NombreProveedor)); }
}
