namespace LogiMarket360.Domain.Entities;

using LogiMarket360.Domain.Exceptions;

public class Producto
{
    public int Id { get; set; }
    public string Sku { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;
    public decimal Precio { get; private set; }
    public int Stock { get; private set; }
    public decimal PesoKg { get; private set; }
    public byte[]? ConcurrencyToken { get; set; }

    protected Producto() { }

    public Producto(string sku, string nombre, decimal precio, int stockInicial, decimal pesoKg)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("El SKU no puede ser vacío.", nameof(sku));
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre no puede ser vacío.", nameof(nombre));
        if (precio <= 0)
            throw new ArgumentOutOfRangeException(nameof(precio), "El precio debe ser un valor monetario positivo.");
        if (stockInicial < 0)
            throw new ArgumentOutOfRangeException(nameof(stockInicial), "El stock no puede ser negativo.");
        if (pesoKg < 0)
            throw new ArgumentOutOfRangeException(nameof(pesoKg), "El peso no puede ser negativo.");

        Sku = sku.Trim().ToUpperInvariant();
        Nombre = nombre.Trim();
        Precio = decimal.Round(precio, 2, MidpointRounding.AwayFromZero);
        Stock = stockInicial;
        PesoKg = decimal.Round(pesoKg, 2, MidpointRounding.AwayFromZero);
    }

    public void DescontarStock(int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad a descontar debe ser mayor a cero.");
        if (Stock < cantidad)
            throw new StockInsuficienteException(Sku, Stock, cantidad);

        Stock -= cantidad;
    }

    public void ReintegrarStock(int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad a reintegrar debe ser mayor a cero.");

        Stock += cantidad;
    }

    public void ActualizarPrecio(decimal nuevoPrecio)
    {
        if (nuevoPrecio <= 0)
            throw new ArgumentOutOfRangeException(nameof(nuevoPrecio), "El precio debe ser un valor monetario positivo.");

        Precio = decimal.Round(nuevoPrecio, 2, MidpointRounding.AwayFromZero);
    }

    public void ActualizarDatos(string nombre, decimal pesoKg)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre no puede ser vacío.", nameof(nombre));
        if (pesoKg < 0)
            throw new ArgumentOutOfRangeException(nameof(pesoKg), "El peso no puede ser negativo.");

        Nombre = nombre.Trim();
        PesoKg = decimal.Round(pesoKg, 2, MidpointRounding.AwayFromZero);
    }
}
