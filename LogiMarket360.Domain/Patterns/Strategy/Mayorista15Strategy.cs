namespace LogiMarket360.Domain.Patterns.Strategy;

public class Mayorista15Strategy : IDescuentoStrategy
{
    public string Nombre => "MAYORISTA";
    public int Prioridad => 1; // Prioridad máxima ante conflicto con VIP

    public bool Aplica(ContextoDescuento contexto)
    {
        return contexto.TotalUnidadesFisicas >= 10;
    }

    public decimal CalcularDescuento(ContextoDescuento contexto)
    {
        decimal descuento = contexto.Subtotal * 0.15m;
        return decimal.Round(descuento, 2, MidpointRounding.AwayFromZero);
    }
}
