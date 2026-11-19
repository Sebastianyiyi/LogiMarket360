namespace LogiMarket360.Domain.Entities;

public class PedidoDetalleComponente
{
    public int Id { get; set; }
    public int PedidoDetalleId { get; set; }
    public string Sku { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;
    public int CantidadFisica { get; private set; }
    public decimal PrecioUnitarioSnapshot { get; private set; }
    public decimal PesoKgSnapshot { get; private set; }

    protected PedidoDetalleComponente() { }

    public PedidoDetalleComponente(
        string sku,
        string nombre,
        int cantidadFisica,
        decimal precioUnitarioSnapshot,
        decimal pesoKgSnapshot)
    {
        Sku = sku.Trim().ToUpperInvariant();
        Nombre = nombre.Trim();
        CantidadFisica = cantidadFisica;
        PrecioUnitarioSnapshot = decimal.Round(precioUnitarioSnapshot, 2, MidpointRounding.AwayFromZero);
        PesoKgSnapshot = decimal.Round(pesoKgSnapshot, 2, MidpointRounding.AwayFromZero);
    }
}
