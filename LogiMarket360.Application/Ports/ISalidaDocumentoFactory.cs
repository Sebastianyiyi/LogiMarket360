namespace LogiMarket360.Application.Ports;

public interface ISalidaDocumentoFactory
{
    ISalidaDocumento Crear(string medio);
    IReadOnlyList<string> MediosDisponibles { get; }
}
