namespace LogiMarket360.Application.Documents;

using LogiMarket360.Application.Ports;

public interface IComprobante
{
    ISalidaDocumento Salida { get; }
    Task<string> EmitirAsync(object datos, CancellationToken ct = default);
    string ObtenerTitulo();
    string ObtenerContenido(object datos);
}

public record DatosComprobanteVenta(
    string NumeroPedido,
    string Cliente,
    string EstrategiaDescuento,
    decimal Subtotal,
    decimal Descuento,
    decimal CostoEnvio,
    decimal Total,
    string ReferenciaPago,
    DateTime Fecha
);

public record DatosComprobanteDevolucion(
    string NumeroPedido,
    string Cliente,
    decimal MontoReembolso,
    string Motivo,
    string ReferenciaReembolso,
    DateTime Fecha
);

public class ComprobanteVenta : IComprobante
{
    public ISalidaDocumento Salida { get; }
    private readonly DatosComprobanteVenta _datos;

    public ComprobanteVenta(ISalidaDocumento salida, DatosComprobanteVenta datos)
    {
        Salida = salida ?? throw new ArgumentNullException(nameof(salida));
        _datos = datos ?? throw new ArgumentNullException(nameof(datos));
    }

    public string ObtenerTitulo() => $"COMPROBANTE DE VENTA - {_datos.NumeroPedido}";

    public string ObtenerContenido(object datos)
    {
        return $"""
            Pedido   : {_datos.NumeroPedido}
            Cliente  : {_datos.Cliente}
            Fecha    : {_datos.Fecha:dd/MM/yyyy HH:mm}
            ---
            Subtotal : USD {_datos.Subtotal:F2}
            Descuento: USD {_datos.Descuento:F2} ({_datos.EstrategiaDescuento})
            Envío    : USD {_datos.CostoEnvio:F2}
            TOTAL    : USD {_datos.Total:F2}
            ---
            Pago Ref : {_datos.ReferenciaPago}
            """;
    }

    public async Task<string> EmitirAsync(object datos, CancellationToken ct = default)
        => await Salida.EmitirAsync(ObtenerTitulo(), ObtenerContenido(datos), ct);
}

public class ComprobanteDevolucion : IComprobante
{
    public ISalidaDocumento Salida { get; }
    private readonly DatosComprobanteDevolucion _datos;

    public ComprobanteDevolucion(ISalidaDocumento salida, DatosComprobanteDevolucion datos)
    {
        Salida = salida ?? throw new ArgumentNullException(nameof(salida));
        _datos = datos ?? throw new ArgumentNullException(nameof(datos));
    }

    public string ObtenerTitulo() => $"COMPROBANTE DE DEVOLUCIÓN - {_datos.NumeroPedido}";

    public string ObtenerContenido(object datos)
    {
        return $"""
            Pedido   : {_datos.NumeroPedido}
            Cliente  : {_datos.Cliente}
            Fecha    : {_datos.Fecha:dd/MM/yyyy HH:mm}
            Motivo   : {_datos.Motivo}
            ---
            Reembolso: USD {_datos.MontoReembolso:F2}
            Ref.Remb.: {_datos.ReferenciaReembolso}
            (Nota: No incluye tarifa de envío)
            """;
    }

    public async Task<string> EmitirAsync(object datos, CancellationToken ct = default)
        => await Salida.EmitirAsync(ObtenerTitulo(), ObtenerContenido(datos), ct);
}
