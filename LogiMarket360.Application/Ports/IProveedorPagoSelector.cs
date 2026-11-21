namespace LogiMarket360.Application.Ports;

public interface IProveedorPagoSelector : IPasarelaPago
{
    IReadOnlyCollection<string> ProveedoresDisponibles { get; }
    string ProveedorActivo { get; }
    bool ModoFalloActivo { get; }

    void Seleccionar(string nombreProveedor);
    void ConfigurarModoFallo(bool activo);
}
