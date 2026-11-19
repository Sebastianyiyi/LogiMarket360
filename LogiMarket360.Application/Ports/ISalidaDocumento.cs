namespace LogiMarket360.Application.Ports;

public interface ISalidaDocumento
{
    string NombreMedio { get; }
    Task<string> EmitirAsync(string titulo, string contenido, CancellationToken ct = default);
}
