namespace LogiMarket360.Domain.Patterns.Strategy;

public class SinDescuentoStrategy : IDescuentoStrategy
{
    public string Nombre => "NORMAL";
    public int Prioridad => 100;

    public bool Aplica(ContextoDescuento contexto) => true;

    public decimal CalcularDescuento(ContextoDescuento contexto) => 0.00m;
}
