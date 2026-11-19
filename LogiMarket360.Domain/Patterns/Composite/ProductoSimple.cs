namespace LogiMarket360.Domain.Patterns.Composite;

using LogiMarket360.Domain.Patterns.Visitor;

public class ProductoSimple : IElementoCatalogo
{
    public string Codigo => Sku;
    public string Sku { get; }
    public string Nombre { get; }
    public decimal Precio { get; }
    public decimal PesoKg { get; }

    public ProductoSimple(string sku, string nombre, decimal precio, decimal pesoKg)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("El SKU no puede ser vacío.", nameof(sku));
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre no puede ser vacío.", nameof(nombre));
        if (precio <= 0)
            throw new ArgumentOutOfRangeException(nameof(precio), "El precio debe ser positivo.");
        if (pesoKg < 0)
            throw new ArgumentOutOfRangeException(nameof(pesoKg), "El peso no puede ser negativo.");

        Sku = sku.Trim().ToUpperInvariant();
        Nombre = nombre.Trim();
        Precio = decimal.Round(precio, 2, MidpointRounding.AwayFromZero);
        PesoKg = decimal.Round(pesoKg, 2, MidpointRounding.AwayFromZero);
    }

    public decimal ObtenerPrecio() => Precio;

    public decimal ObtenerPeso() => PesoKg;

    public IReadOnlyList<ComponenteDesglosado> DesglosarComponentes(int factor = 1)
    {
        if (factor <= 0)
            throw new ArgumentOutOfRangeException(nameof(factor), "El factor debe ser mayor a cero.");

        return new List<ComponenteDesglosado>
        {
            new(Sku, Nombre, Precio, PesoKg, factor)
        };
    }

    public void Aceptar(ICatalogoVisitor visitor, int cantidad = 1)
    {
        visitor.VisitarProducto(this, cantidad);
    }

    public bool ContieneElemento(string codigo)
    {
        return string.Equals(Sku, codigo, StringComparison.OrdinalIgnoreCase);
    }
}
