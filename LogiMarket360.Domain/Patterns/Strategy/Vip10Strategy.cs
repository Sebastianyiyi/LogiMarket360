namespace LogiMarket360.Domain.Patterns.Strategy;

using LogiMarket360.Domain.Enums;

public class Vip10Strategy : IDescuentoStrategy
{
    public string Nombre => "VIP";
    public int Prioridad => 2;

    public bool Aplica(ContextoDescuento contexto)
    {
        return contexto.CategoriaCliente == CategoriaCliente.VIP;
    }

    public decimal CalcularDescuento(ContextoDescuento contexto)
    {
        decimal descuento = contexto.Subtotal * 0.10m;
        return decimal.Round(descuento, 2, MidpointRounding.AwayFromZero);
    }
}
