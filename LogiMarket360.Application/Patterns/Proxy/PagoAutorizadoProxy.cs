namespace LogiMarket360.Application.Patterns.Proxy;

using LogiMarket360.Application.DTOs;
using LogiMarket360.Application.Ports;
using LogiMarket360.Domain.Enums;

public class PagoAutorizadoProxy : IPasarelaPago
{
    private readonly IPasarelaPago _pasarelaDestino;
    private readonly Func<RolUsuario> _obtenerRolActual;

    public string NombreProveedor => _pasarelaDestino.NombreProveedor;

    public PagoAutorizadoProxy(IPasarelaPago pasarelaDestino, Func<RolUsuario> obtenerRolActual)
    {
        _pasarelaDestino = pasarelaDestino ?? throw new ArgumentNullException(nameof(pasarelaDestino));
        _obtenerRolActual = obtenerRolActual ?? throw new ArgumentNullException(nameof(obtenerRolActual));
    }

    public async Task<ResultadoPago> CobrarAsync(decimal monto, string claveIdempotencia, CancellationToken ct = default)
    {
        var rol = _obtenerRolActual();

        // Control de autorización estricto: solo ADMIN y VENTAS tienen permiso para ejecutar cobros
        if (rol != RolUsuario.ADMIN && rol != RolUsuario.VENTAS)
        {
            throw new UnauthorizedAccessException(
                $"Acceso denegado: El rol '{rol}' no tiene autorización para procesar operaciones de cobro.");
        }

        // Si está autorizado, delega en la siguiente capa (Decorator/Pasarela)
        return await _pasarelaDestino.CobrarAsync(monto, claveIdempotencia, ct);
    }
}
