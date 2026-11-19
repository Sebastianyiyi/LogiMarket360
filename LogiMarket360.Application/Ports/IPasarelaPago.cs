namespace LogiMarket360.Application.Ports;

using LogiMarket360.Application.DTOs;

public interface IPasarelaPago
{
    string NombreProveedor { get; }
    Task<ResultadoPago> CobrarAsync(decimal monto, string claveIdempotencia, CancellationToken ct = default);
}
