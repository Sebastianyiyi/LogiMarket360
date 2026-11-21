namespace LogiMarket360.Infrastructure.Adapters;

using LogiMarket360.Application.DTOs;
using LogiMarket360.Application.Ports;

// Simulador de pasarela legada con interfaz incompatible (CobrarCentavos)
// Conserva el contrato legado; Application solo lo conoce mediante el Adapter.
public class PasarelaLegadaSimulada
{
    public string ProviderName => "PasarelaLegada-v1";

    public (long Referencia, bool Autorizado, string Mensaje) CobrarCentavos(long montoEnCentavos, string idTransaccion)
    {
        long referencia = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return (referencia, true, "Transacción aprobada por la pasarela legada.");
    }
}

// ADAPTER: adapta PasarelaLegadaSimulada a IPasarelaPago sin modificar el legado
public class PasarelaLegadaAdapter : IPasarelaPago
{
    private readonly PasarelaLegadaSimulada _legada;

    public string NombreProveedor => "LEGADA";

    public PasarelaLegadaAdapter(PasarelaLegadaSimulada legada)
    {
        _legada = legada ?? throw new ArgumentNullException(nameof(legada));
    }

    public Task<ResultadoPago> CobrarAsync(decimal monto, string claveIdempotencia, CancellationToken ct = default)
    {
        // Adaptación: convertir decimal a centavos (long) y retornar ResultadoPago
        long montoCentavos = (long)(monto * 100);
        var (referencia, aprobado, mensaje) = _legada.CobrarCentavos(montoCentavos, claveIdempotencia);

        var resultado = new ResultadoPago(
            Exitoso: aprobado,
            ReferenciaTransaccion: aprobado ? $"LEG-{referencia}" : string.Empty,
            Mensaje: mensaje,
            MontoCobrado: aprobado ? monto : 0m,
            Proveedor: NombreProveedor
        );

        return Task.FromResult(resultado);
    }
}

// Segunda pasarela simulada alternativa (sustituible mediante DI)
public class PasarelaModernaSimulada : IPasarelaPago
{
    public string NombreProveedor => "MODERNA";

    public Task<ResultadoPago> CobrarAsync(decimal monto, string claveIdempotencia, CancellationToken ct = default)
    {
        string referencia = $"MOD-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}";

        return Task.FromResult(new ResultadoPago(
            Exitoso: true,
            ReferenciaTransaccion: referencia,
            Mensaje: "Cobro aprobado por la pasarela moderna.",
            MontoCobrado: monto,
            Proveedor: NombreProveedor
        ));
    }
}

// Pasarela que siempre falla (para pruebas P06/P36/P37)
public class PasarelaFallaSimulada : IPasarelaPago
{
    public string NombreProveedor => "FALLA_TEST";

    public Task<ResultadoPago> CobrarAsync(decimal monto, string claveIdempotencia, CancellationToken ct = default)
    {
        return Task.FromResult(new ResultadoPago(
            Exitoso: false,
            ReferenciaTransaccion: string.Empty,
            Mensaje: "Cobro rechazado (modo de prueba de fallo activado).",
            MontoCobrado: 0m,
            Proveedor: NombreProveedor
        ));
    }
}
