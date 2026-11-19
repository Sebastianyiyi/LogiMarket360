namespace LogiMarket360.Domain.Entities;

public class PedidoDetalle
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public string CodigoItem { get; private set; } = string.Empty;
    public string NombreItem { get; private set; } = string.Empty;
    public bool EsKit { get; private set; }
    public int Cantidad { get; private set; }
    public decimal PrecioUnitarioSnapshot { get; private set; }
    public decimal SubtotalLinea { get; private set; }

    private readonly List<PedidoDetalleComponente> _componentesSnapshot = new();
    public virtual IReadOnlyCollection<PedidoDetalleComponente> ComponentesSnapshot => _componentesSnapshot.AsReadOnly();

    protected PedidoDetalle() { }

    public PedidoDetalle(
        string codigoItem,
        string nombreItem,
        bool esKit,
        int cantidad,
        decimal precioUnitarioSnapshot,
        IEnumerable<PedidoDetalleComponente>? componentes = null)
    {
        if (string.IsNullOrWhiteSpace(codigoItem))
            throw new ArgumentException("El código del ítem no puede ser vacío.", nameof(codigoItem));
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor a cero.");
        if (precioUnitarioSnapshot <= 0)
            throw new ArgumentOutOfRangeException(nameof(precioUnitarioSnapshot), "El precio debe ser positivo.");

        CodigoItem = codigoItem.Trim().ToUpperInvariant();
        NombreItem = nombreItem.Trim();
        EsKit = esKit;
        Cantidad = cantidad;
        PrecioUnitarioSnapshot = decimal.Round(precioUnitarioSnapshot, 2, MidpointRounding.AwayFromZero);
        SubtotalLinea = decimal.Round(PrecioUnitarioSnapshot * Cantidad, 2, MidpointRounding.AwayFromZero);

        if (componentes != null)
        {
            _componentesSnapshot.AddRange(componentes);
        }
    }

    public void AgregarComponenteSnapshot(PedidoDetalleComponente componente)
    {
        _componentesSnapshot.Add(componente);
    }
}
