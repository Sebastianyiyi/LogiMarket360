namespace LogiMarket360.Infrastructure.Delivery;

using LogiMarket360.Domain.Enums;

// Abstract Factory: productos abstractos de entrega
public interface ITarifaCalculo { decimal CalcularTarifa(); }
public interface IPlazoCalculo { int CalcularPlazoEnDias(); }
public interface IGuiaGenerador { string GenerarGuia(string numeroPedido); }

// Abstract Factory: fábrica abstracta
public interface IEntregaFactory
{
    TipoEntrega TipoEntrega { get; }
    ITarifaCalculo CrearCalculadorTarifa();
    IPlazoCalculo CrearCalculadorPlazo();
    IGuiaGenerador CrearGeneradorGuia();
}

// --- ESTÁNDAR ---
public class TarifaEstandar : ITarifaCalculo { public decimal CalcularTarifa() => 5.00m; }
public class PlazoEstandar : IPlazoCalculo { public int CalcularPlazoEnDias() => 3; }
public class GuiaEstandar : IGuiaGenerador
{
    public string GenerarGuia(string numeroPedido) =>
        $"EST-{numeroPedido}-{DateTime.UtcNow:yyyyMMddHHmm}";
}

public class EntregaEstandarFactory : IEntregaFactory
{
    public TipoEntrega TipoEntrega => TipoEntrega.ESTANDAR;
    public ITarifaCalculo CrearCalculadorTarifa() => new TarifaEstandar();
    public IPlazoCalculo CrearCalculadorPlazo() => new PlazoEstandar();
    public IGuiaGenerador CrearGeneradorGuia() => new GuiaEstandar();
}

// --- EXPRÉS ---
public class TarifaExpres : ITarifaCalculo { public decimal CalcularTarifa() => 12.00m; }
public class PlazoExpres : IPlazoCalculo { public int CalcularPlazoEnDias() => 1; }
public class GuiaExpres : IGuiaGenerador
{
    public string GenerarGuia(string numeroPedido) =>
        $"EXP-{numeroPedido}-{DateTime.UtcNow:yyyyMMddHHmmss}";
}

public class EntregaExpresFactory : IEntregaFactory
{
    public TipoEntrega TipoEntrega => TipoEntrega.EXPRES;
    public ITarifaCalculo CrearCalculadorTarifa() => new TarifaExpres();
    public IPlazoCalculo CrearCalculadorPlazo() => new PlazoExpres();
    public IGuiaGenerador CrearGeneradorGuia() => new GuiaExpres();
}

// Selector de fábrica
public static class EntregaFactorySelector
{
    public static IEntregaFactory ObtenerFactory(TipoEntrega tipo) => tipo switch
    {
        TipoEntrega.ESTANDAR => new EntregaEstandarFactory(),
        TipoEntrega.EXPRES => new EntregaExpresFactory(),
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), $"Tipo de entrega no soportado: {tipo}")
    };
}
