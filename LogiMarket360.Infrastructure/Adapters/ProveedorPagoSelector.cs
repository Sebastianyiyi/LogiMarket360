namespace LogiMarket360.Infrastructure.Adapters;

using LogiMarket360.Application.DTOs;
using LogiMarket360.Application.Ports;

public sealed class ProveedorPagoSelector : IProveedorPagoSelector
{
    private readonly IReadOnlyDictionary<string, IPasarelaPago> _proveedores;
    private readonly object _sync = new();
    private string _proveedorActivo;
    private bool _modoFalloActivo;

    public ProveedorPagoSelector(
        PasarelaLegadaAdapter pasarelaLegada,
        PasarelaModernaSimulada pasarelaModerna)
    {
        ArgumentNullException.ThrowIfNull(pasarelaLegada);
        ArgumentNullException.ThrowIfNull(pasarelaModerna);

        _proveedores = new Dictionary<string, IPasarelaPago>(StringComparer.OrdinalIgnoreCase)
        {
            [pasarelaLegada.NombreProveedor] = pasarelaLegada,
            [pasarelaModerna.NombreProveedor] = pasarelaModerna
        };
        _proveedorActivo = pasarelaLegada.NombreProveedor;
    }

    public IReadOnlyCollection<string> ProveedoresDisponibles => _proveedores.Keys.ToArray();

    public string ProveedorActivo
    {
        get { lock (_sync) return _proveedorActivo; }
    }

    public string NombreProveedor => ProveedorActivo;

    public bool ModoFalloActivo
    {
        get { lock (_sync) return _modoFalloActivo; }
    }

    public void Seleccionar(string nombreProveedor)
    {
        if (string.IsNullOrWhiteSpace(nombreProveedor))
            throw new ArgumentException("Debe indicar el proveedor de pago.", nameof(nombreProveedor));

        string normalizado = nombreProveedor.Trim().ToUpperInvariant();
        if (!_proveedores.ContainsKey(normalizado))
        {
            throw new ArgumentOutOfRangeException(
                nameof(nombreProveedor),
                nombreProveedor,
                $"Proveedor no soportado. Opciones: {string.Join(", ", ProveedoresDisponibles)}.");
        }

        lock (_sync) _proveedorActivo = normalizado;
    }

    public void ConfigurarModoFallo(bool activo)
    {
        lock (_sync) _modoFalloActivo = activo;
    }

    public async Task<ResultadoPago> CobrarAsync(
        decimal monto,
        string claveIdempotencia,
        CancellationToken ct = default)
    {
        IPasarelaPago proveedor;
        bool inducirFallo;
        lock (_sync)
        {
            proveedor = _proveedores[_proveedorActivo];
            inducirFallo = _modoFalloActivo;
        }

        if (inducirFallo)
        {
            return new ResultadoPago(
                Exitoso: false,
                ReferenciaTransaccion: string.Empty,
                Mensaje: $"Cobro rechazado por modo de fallo controlado en {proveedor.NombreProveedor}.",
                MontoCobrado: 0m,
                Proveedor: proveedor.NombreProveedor);
        }

        return await proveedor.CobrarAsync(monto, claveIdempotencia, ct);
    }
}
