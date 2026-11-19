namespace LogiMarket360.Application.Patterns.Decorator;

using LogiMarket360.Application.DTOs;
using LogiMarket360.Application.Ports;

public class AuditoriaPagoDecorator : IPasarelaPago
{
    private readonly IPasarelaPago _inner;
    private readonly Action<string>? _logAction;
    private readonly List<string> _bitacoraInterna = new();

    public string NombreProveedor => _inner.NombreProveedor;
    public IReadOnlyList<string> Bitacora => _bitacoraInterna.AsReadOnly();

    public AuditoriaPagoDecorator(IPasarelaPago inner, Action<string>? logAction = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logAction = logAction;
    }

    public async Task<ResultadoPago> CobrarAsync(decimal monto, string claveIdempotencia, CancellationToken ct = default)
    {
        string correlacionId = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        RegistrarLog($"[COBRO-INICIO] [ID:{correlacionId}] Pasarela: {_inner.NombreProveedor} | Monto: USD {monto:F2} | Clave: {claveIdempotencia}");

        try
        {
            var resultado = await _inner.CobrarAsync(monto, claveIdempotencia, ct);

            if (resultado.Exitoso)
            {
                RegistrarLog($"[COBRO-FIN-EXITO] [ID:{correlacionId}] Ref: {resultado.ReferenciaTransaccion} | Proveedor: {resultado.Proveedor}");
            }
            else
            {
                RegistrarLog($"[COBRO-FIN-RECHAZO] [ID:{correlacionId}] Mensaje: {resultado.Mensaje}");
            }

            return resultado;
        }
        catch (Exception ex)
        {
            RegistrarLog($"[COBRO-ERROR] [ID:{correlacionId}] Excepción: {ex.Message}");
            throw;
        }
    }

    private void RegistrarLog(string mensaje)
    {
        _bitacoraInterna.Add(mensaje);
        _logAction?.Invoke(mensaje);
    }
}
