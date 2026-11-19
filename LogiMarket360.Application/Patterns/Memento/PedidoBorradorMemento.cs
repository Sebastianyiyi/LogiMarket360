namespace LogiMarket360.Application.Patterns.Memento;

using LogiMarket360.Domain.Entities;
using LogiMarket360.Domain.Enums;

public record DetalleMemento(
    string CodigoItem,
    string NombreItem,
    bool EsKit,
    int Cantidad,
    decimal PrecioUnitarioSnapshot,
    List<ComponenteMemento> Componentes
);

public record ComponenteMemento(
    string Sku,
    string Nombre,
    int CantidadFisica,
    decimal PrecioUnitarioSnapshot,
    decimal PesoKgSnapshot
);

public class PedidoBorradorMemento
{
    public string NumeroPedido { get; }
    public int ClienteId { get; }
    public TipoEntrega TipoEntrega { get; }
    public decimal CostoEnvio { get; }
    public decimal Subtotal { get; }
    public decimal Descuento { get; }
    public decimal Total { get; }
    public string EstrategiaDescuento { get; }
    public IReadOnlyList<DetalleMemento> Detalles { get; }
    public DateTime Timestamp { get; }

    public PedidoBorradorMemento(Pedido pedido)
    {
        ArgumentNullException.ThrowIfNull(pedido);

        NumeroPedido = pedido.NumeroPedido;
        ClienteId = pedido.ClienteId;
        TipoEntrega = pedido.TipoEntrega;
        CostoEnvio = pedido.CostoEnvio;
        Subtotal = pedido.Subtotal;
        Descuento = pedido.Descuento;
        Total = pedido.Total;
        EstrategiaDescuento = pedido.EstrategiaDescuento;
        Timestamp = DateTime.UtcNow;

        var detalles = new List<DetalleMemento>();
        foreach (var d in pedido.Detalles)
        {
            var comps = d.ComponentesSnapshot.Select(c => new ComponenteMemento(
                c.Sku, c.Nombre, c.CantidadFisica, c.PrecioUnitarioSnapshot, c.PesoKgSnapshot
            )).ToList();

            detalles.Add(new DetalleMemento(
                d.CodigoItem, d.NombreItem, d.EsKit, d.Cantidad, d.PrecioUnitarioSnapshot, comps
            ));
        }
        Detalles = detalles;
    }

    public Pedido Restaurar()
    {
        var pedido = new Pedido(NumeroPedido, ClienteId, TipoEntrega, CostoEnvio);

        foreach (var dm in Detalles)
        {
            var comps = dm.Componentes.Select(c => new PedidoDetalleComponente(
                c.Sku, c.Nombre, c.CantidadFisica, c.PrecioUnitarioSnapshot, c.PesoKgSnapshot
            )).ToList();

            var detalle = new PedidoDetalle(
                dm.CodigoItem, dm.NombreItem, dm.EsKit, dm.Cantidad, dm.PrecioUnitarioSnapshot, comps
            );
            pedido.AgregarDetalle(detalle);
        }

        pedido.RecalcularTotales(Descuento, EstrategiaDescuento);
        return pedido;
    }
}
