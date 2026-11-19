namespace LogiMarket360.Domain.Entities;

using LogiMarket360.Domain.Enums;

public class MovimientoInventario
{
    public int Id { get; set; }
    public int ProductoId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public int Cantidad { get; private set; } // Negativo para salidas/ventas, positivo para ingresos/reintegros
    public TipoMovimientoInventario TipoMovimiento { get; private set; }
    public string Motivo { get; private set; } = string.Empty;
    public string Referencia { get; private set; } = string.Empty;
    public DateTime Fecha { get; private set; }

    protected MovimientoInventario() { }

    public MovimientoInventario(
        int productoId,
        string sku,
        int cantidad,
        TipoMovimientoInventario tipoMovimiento,
        string motivo,
        string referencia)
    {
        if (cantidad == 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad de movimiento no puede ser cero.");

        ProductoId = productoId;
        Sku = sku.Trim().ToUpperInvariant();
        Cantidad = cantidad;
        TipoMovimiento = tipoMovimiento;
        Motivo = motivo.Trim();
        Referencia = referencia.Trim();
        Fecha = DateTime.UtcNow;
    }
}
