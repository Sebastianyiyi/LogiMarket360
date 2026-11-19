namespace LogiMarket360.Domain.Entities;

using LogiMarket360.Domain.Enums;

public class Despacho
{
    public int Id { get; set; }
    public int PedidoId { get; private set; }
    public string NumeroGuia { get; private set; } = string.Empty;
    public string ProveedorLogistico { get; private set; } = string.Empty;
    public TipoEntrega TipoEntrega { get; private set; }
    public int PlazoEstimadoDias { get; private set; }
    public DateTime FechaDespacho { get; private set; }
    public DateTime FechaEntregaEstimada { get; private set; }
    public DateTime? FechaEntregaReal { get; private set; }

    protected Despacho() { }

    public Despacho(
        int pedidoId,
        string numeroGuia,
        string proveedorLogistico,
        TipoEntrega tipoEntrega,
        int plazoEstimadoDias)
    {
        PedidoId = pedidoId;
        NumeroGuia = numeroGuia.Trim().ToUpperInvariant();
        ProveedorLogistico = proveedorLogistico.Trim();
        TipoEntrega = tipoEntrega;
        PlazoEstimadoDias = plazoEstimadoDias;
        FechaDespacho = DateTime.UtcNow;
        FechaEntregaEstimada = FechaDespacho.AddDays(plazoEstimadoDias);
    }

    public void MarcarEntregado(DateTime? fechaReal = null)
    {
        FechaEntregaReal = fechaReal ?? DateTime.UtcNow;
    }
}
