using LogiMarket360.Application.DTOs;
using LogiMarket360.Application.Patterns.Command;
using LogiMarket360.Application.Patterns.Decorator;
using LogiMarket360.Application.Patterns.Flyweight;
using LogiMarket360.Application.Patterns.Interpreter;
using LogiMarket360.Application.Patterns.Proxy;
using LogiMarket360.Application.Patterns.Singleton;
using LogiMarket360.Application.Ports;
using LogiMarket360.Domain.Enums;
using LogiMarket360.Domain.Exceptions;
using LogiMarket360.Domain.Patterns.Composite;
using LogiMarket360.Domain.Patterns.Strategy;
using LogiMarket360.Infrastructure.Adapters;
using LogiMarket360.Infrastructure.Delivery;

namespace LogiMarket360.Tests;

public sealed class PatternBehaviorTests
{
    [Theory]
    [InlineData("", "Producto", 1, 0)]
    [InlineData("P01", "", 1, 0)]
    [InlineData("P01", "Producto", 0, 0)]
    [InlineData("P01", "Producto", 1, -1)]
    public void ProductoSimple_RechazaDatosInvalidos(string sku, string nombre, decimal precio, decimal peso) =>
        Assert.ThrowsAny<ArgumentException>(() => new ProductoSimple(sku, nombre, precio, peso));

    [Fact]
    public void Composite_CalculaKitsOficiales()
    {
        var oficina = Oficina();
        var teletrabajo = new KitCompuesto("KIT_TELETRABAJO", "Teletrabajo");
        teletrabajo.AgregarComponente(oficina);
        teletrabajo.AgregarComponente(new ProductoSimple("P03", "Memoria", 10m, .1m));
        Assert.Equal(50m, oficina.ObtenerPrecio());
        Assert.Equal(60m, teletrabajo.ObtenerPrecio());
        Assert.Equal(1m, teletrabajo.ObtenerPeso());
    }

    [Fact]
    public void Composite_ExpandeKitAnidadoConFactor()
    {
        var teletrabajo = new KitCompuesto("KIT_TELETRABAJO", "Teletrabajo");
        teletrabajo.AgregarComponente(Oficina());
        teletrabajo.AgregarComponente(new ProductoSimple("P03", "Memoria", 10m, .1m));
        var componentes = teletrabajo.DesglosarComponentes(2);
        Assert.Equal(3, componentes.Count);
        Assert.All(componentes, c => Assert.Equal(2, c.Cantidad));
    }

    [Fact]
    public void Composite_RechazaCicloIndirecto()
    {
        var padre = new KitCompuesto("PADRE", "Padre");
        var hijo = new KitCompuesto("HIJO", "Hijo");
        padre.AgregarComponente(hijo);
        Assert.Throws<CicloDetectadoException>(() => hijo.AgregarComponente(padre));
    }

    [Theory]
    [InlineData(CategoriaCliente.VIP, 3, 60, "VIP", 6)]
    [InlineData(CategoriaCliente.VIP, 10, 100, "MAYORISTA", 15)]
    [InlineData(CategoriaCliente.NORMAL, 10, 100, "MAYORISTA", 15)]
    [InlineData(CategoriaCliente.NORMAL, 2, 90, "NORMAL", 0)]
    public void Strategy_RespetaPrioridadYMontos(CategoriaCliente categoria, int unidades, decimal subtotal, string nombre, decimal descuento)
    {
        var resultado = new CalculadoraDescuento().Calcular(new ContextoDescuento(categoria, unidades, subtotal));
        Assert.Equal(nombre, resultado.NombreEstrategia);
        Assert.Equal(descuento, resultado.MontoDescuento);
    }

    [Theory]
    [InlineData(TipoEntrega.ESTANDAR, 5, 3, "EST-")]
    [InlineData(TipoEntrega.EXPRES, 12, 1, "EXP-")]
    public void AbstractFactory_EntregaFamiliasCoherentes(TipoEntrega tipo, decimal tarifa, int dias, string prefijo)
    {
        var fabrica = new EntregaFactoryProvider().Obtener(tipo);
        Assert.Equal(tarifa, fabrica.CrearCalculadorTarifa().CalcularTarifa());
        Assert.Equal(dias, fabrica.CrearCalculadorPlazo().CalcularPlazoEnDias());
        Assert.StartsWith(prefijo, fabrica.CrearGeneradorGuia().GenerarGuia("PED-1"));
    }

    [Fact]
    public void Flyweight_ComparteMetadatosEnMilFilas()
    {
        var factory = new CatalogoVisualFlyweightFactory();
        var filas = factory.GenerarFilasVista(Enumerable.Range(0, 1000).Select(i => ("P01", "Mouse", "Accesorios", "USB", i, 20m)));
        Assert.Equal(1000, filas.Count);
        Assert.Equal(1, factory.TotalInstanciasEnCache);
        Assert.True(ReferenceEquals(filas[0].Metadatos, filas[999].Metadatos));
    }

    [Fact]
    public async Task Proxy_DenegaConsultaSinInvocarPasarela()
    {
        var destino = new PasarelaContadora();
        var proxy = new PagoAutorizadoProxy(destino, () => RolUsuario.CONSULTA);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => proxy.CobrarAsync(10m, "key"));
        Assert.Equal(0, destino.Invocaciones);
    }

    [Fact]
    public async Task Decorator_RegistraInicioYFin()
    {
        var logs = new List<string>();
        var decorator = new AuditoriaPagoDecorator(new PasarelaContadora(), logs.Add);
        var resultado = await decorator.CobrarAsync(10m, "key");
        Assert.True(resultado.Exitoso);
        Assert.Equal(2, logs.Count);
    }

    [Fact]
    public async Task CommandInvoker_RegistraResultado()
    {
        var invoker = new CommandInvoker();
        var resultado = await invoker.InvocarAsync(new ComandoFalso());
        Assert.True(resultado.Exitoso);
        Assert.Single(invoker.Historial);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ESTADO")]
    [InlineData("TOTAL >= nada")]
    [InlineData("( ESTADO = PAGADO")]
    public void Interpreter_RechazaSintaxisInvalida(string expresion) =>
        Assert.Throws<FormatException>(() => new PedidoQueryParser().Parse(expresion));

    [Fact]
    public async Task PasarelaDeFallo_NuncaCobra()
    {
        var resultado = await new PasarelaFallaSimulada().CobrarAsync(50m, "key");
        Assert.False(resultado.Exitoso);
        Assert.Equal(0m, resultado.MontoCobrado);
    }

    [Fact]
    public void Singleton_EntregaMismaInstancia() =>
        Assert.Same(MetricasSesion.Instancia, MetricasSesion.Instancia);

    private static KitCompuesto Oficina()
    {
        var kit = new KitCompuesto("KIT_OFICINA", "Oficina");
        kit.AgregarComponente(new ProductoSimple("P01", "Mouse", 20m, .2m));
        kit.AgregarComponente(new ProductoSimple("P02", "Teclado", 30m, .7m));
        return kit;
    }

    private sealed class PasarelaContadora : IPasarelaPago
    {
        public int Invocaciones { get; private set; }
        public string NombreProveedor => "FAKE";
        public Task<ResultadoPago> CobrarAsync(decimal monto, string claveIdempotencia, CancellationToken ct = default)
        {
            Invocaciones++;
            return Task.FromResult(new ResultadoPago(true, "REF", "OK", monto, NombreProveedor));
        }
    }

    private sealed class ComandoFalso : ICommand
    {
        public string Nombre => "Falso";
        public Task<CommandResult> ExecuteAsync(CancellationToken ct = default) => Task.FromResult(CommandResult.Ok("OK"));
    }
}
