namespace LogiMarket360.Domain.Patterns.Strategy;

public interface IDescuentoStrategy
{
    string Nombre { get; }
    int Prioridad { get; } // Menor número = mayor prioridad
    bool Aplica(ContextoDescuento contexto);
    decimal CalcularDescuento(ContextoDescuento contexto);
}
