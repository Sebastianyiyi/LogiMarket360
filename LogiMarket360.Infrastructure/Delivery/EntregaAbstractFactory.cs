namespace LogiMarket360.Infrastructure.Delivery;

using LogiMarket360.Application.Ports;
using LogiMarket360.Domain.Enums;

// Abstract Factory: fábricas concretas de las familias ESTÁNDAR y EXPRÉS.
public class TarifaEstandar : ITarifaCalculo
{
    public decimal CalcularTarifa() => 5.00m;
}

public class PlazoEstandar : IPlazoCalculo
{
    public int CalcularPlazoEnDias() => 3;
}

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

public class TarifaExpres : ITarifaCalculo
{
    public decimal CalcularTarifa() => 12.00m;
}

public class PlazoExpres : IPlazoCalculo
{
    public int CalcularPlazoEnDias() => 1;
}

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

public class EntregaFactoryProvider : IEntregaFactoryProvider
{
    public IEntregaFactory Obtener(TipoEntrega tipoEntrega) => tipoEntrega switch
    {
        TipoEntrega.ESTANDAR => new EntregaEstandarFactory(),
        TipoEntrega.EXPRES => new EntregaExpresFactory(),
        _ => throw new ArgumentOutOfRangeException(nameof(tipoEntrega), $"Tipo de entrega no soportado: {tipoEntrega}")
    };
}
