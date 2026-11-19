namespace LogiMarket360.Domain.Entities;

public class KitComponente
{
    public int Id { get; set; }
    public int KitPadreId { get; private set; }
    public string TipoElemento { get; private set; } = string.Empty; // "PRODUCTO" o "KIT"
    public string ElementoCodigo { get; private set; } = string.Empty; // SKU o CodigoKit
    public int Cantidad { get; private set; }

    protected KitComponente() { }

    public KitComponente(int kitPadreId, string tipoElemento, string elementoCodigo, int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor a cero.");

        KitPadreId = kitPadreId;
        TipoElemento = tipoElemento.Trim().ToUpperInvariant();
        ElementoCodigo = elementoCodigo.Trim().ToUpperInvariant();
        Cantidad = cantidad;
    }
}
