namespace LogiMarket360.Domain.Entities;

public class Pago
{
    public int Id { get; set; }
    public int PedidoId { get; private set; }
    public decimal Monto { get; private set; }
    public string ReferenciaTransaccion { get; private set; } = string.Empty;
    public string ClaveIdempotencia { get; private set; } = string.Empty;
    public string MetodoPago { get; private set; } = string.Empty;
    public string ProveedorSimulado { get; private set; } = string.Empty;
    public DateTime FechaPago { get; private set; }
    public bool Aprobado { get; private set; }

    protected Pago() { }

    public Pago(
        int pedidoId,
        decimal monto,
        string referenciaTransaccion,
        string claveIdempotencia,
        string metodoPago,
        string proveedorSimulado,
        bool aprobado)
    {
        if (monto <= 0)
            throw new ArgumentOutOfRangeException(nameof(monto), "El monto del pago debe ser positivo.");
        if (string.IsNullOrWhiteSpace(referenciaTransaccion))
            throw new ArgumentException("La referencia no puede ser vacía.", nameof(referenciaTransaccion));

        PedidoId = pedidoId;
        Monto = decimal.Round(monto, 2, MidpointRounding.AwayFromZero);
        ReferenciaTransaccion = referenciaTransaccion.Trim();
        ClaveIdempotencia = claveIdempotencia.Trim();
        MetodoPago = metodoPago.Trim();
        ProveedorSimulado = proveedorSimulado.Trim();
        FechaPago = DateTime.UtcNow;
        Aprobado = aprobado;
    }
}
