namespace LogiMarket360.Domain.Patterns.Strategy;

using LogiMarket360.Domain.Enums;

public record ContextoDescuento(
    CategoriaCliente CategoriaCliente,
    int TotalUnidadesFisicas,
    decimal Subtotal
);
