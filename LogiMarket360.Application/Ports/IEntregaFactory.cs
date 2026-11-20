namespace LogiMarket360.Application.Ports;

using LogiMarket360.Domain.Enums;

public interface ITarifaCalculo
{
    decimal CalcularTarifa();
}

public interface IPlazoCalculo
{
    int CalcularPlazoEnDias();
}

public interface IGuiaGenerador
{
    string GenerarGuia(string numeroPedido);
}

public interface IEntregaFactory
{
    TipoEntrega TipoEntrega { get; }
    ITarifaCalculo CrearCalculadorTarifa();
    IPlazoCalculo CrearCalculadorPlazo();
    IGuiaGenerador CrearGeneradorGuia();
}

public interface IEntregaFactoryProvider
{
    IEntregaFactory Obtener(TipoEntrega tipoEntrega);
}
