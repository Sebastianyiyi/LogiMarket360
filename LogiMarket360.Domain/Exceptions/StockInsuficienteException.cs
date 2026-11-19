namespace LogiMarket360.Domain.Exceptions;

public class StockInsuficienteException : DomainException
{
    public string Sku { get; }
    public int StockDisponible { get; }
    public int CantidadRequerida { get; }

    public StockInsuficienteException(string sku, int stockDisponible, int cantidadRequerida)
        : base($"Stock insuficiente para el producto '{sku}'. Disponible: {stockDisponible}, Requerido: {cantidadRequerida}.")
    {
        Sku = sku;
        StockDisponible = stockDisponible;
        CantidadRequerida = cantidadRequerida;
    }
}
