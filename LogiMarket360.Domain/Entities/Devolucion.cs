namespace LogiMarket360.Domain.Entities;

using LogiMarket360.Domain.Enums;

public class Devolucion
{
    public int Id { get; set; }
    public int PedidoId { get; private set; }
    public MotivoDevolucion Motivo { get; private set; }
    public DateTime FechaSolicitud { get; private set; }
    public int DiasDesdeEntrega { get; private set; }
    public bool ProductoSinUso { get; private set; }
    public bool Aprobada { get; private set; }
    public decimal MontoReembolso { get; private set; }
    public bool ReintegroStockRealizado { get; private set; }
    public string DetalleMotivo { get; private set; } = string.Empty;
    public string ReferenciaReembolso { get; private set; } = string.Empty;

    protected Devolucion() { }

    public Devolucion(
        int pedidoId,
        MotivoDevolucion motivo,
        int diasDesdeEntrega,
        bool productoSinUso,
        string detalleMotivo,
        decimal subtotalMenosDescuento)
    {
        PedidoId = pedidoId;
        Motivo = motivo;
        DiasDesdeEntrega = diasDesdeEntrega;
        ProductoSinUso = productoSinUso;
        DetalleMotivo = detalleMotivo.Trim();
        FechaSolicitud = DateTime.UtcNow;
        MontoReembolso = decimal.Round(subtotalMenosDescuento, 2, MidpointRounding.AwayFromZero);
        Aprobada = false;
        ReintegroStockRealizado = false;
        ReferenciaReembolso = string.Empty;
    }

    public void Aprobar(string referenciaReembolso)
    {
        Aprobada = true;
        ReintegroStockRealizado = true;
        ReferenciaReembolso = referenciaReembolso.Trim();
    }

    public void Rechazar(string motivoRechazo)
    {
        Aprobada = false;
        ReintegroStockRealizado = false;
        MontoReembolso = 0m;
        DetalleMotivo = $"{DetalleMotivo} | Rechazado: {motivoRechazo}".Trim();
    }
}
