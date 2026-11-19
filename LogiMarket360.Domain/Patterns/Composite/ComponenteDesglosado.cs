namespace LogiMarket360.Domain.Patterns.Composite;

public record ComponenteDesglosado(
    string Sku,
    string Nombre,
    decimal PrecioUnitario,
    decimal PesoKg,
    int Cantidad
);
