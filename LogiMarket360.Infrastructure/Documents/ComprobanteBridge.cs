namespace LogiMarket360.Infrastructure.Documents;

using LogiMarket360.Application.Ports;

// BRIDGE: las abstracciones IComprobante viven en Application; aquí se implementan
// los medios concretos de salida (Implementor).
public class SalidaVistaPreviaTxt : ISalidaDocumento
{
    private readonly List<string> _vistasEmitidas = new();
    public IReadOnlyList<string> VistasEmitidas => _vistasEmitidas.AsReadOnly();
    public string NombreMedio => "VISTA_PREVIA";

    public Task<string> EmitirAsync(string titulo, string contenido, CancellationToken ct = default)
    {
        string vista = $"\n{'=',40}\n{titulo}\n{'=',40}\n{contenido}\n{'=',40}";
        _vistasEmitidas.Add(vista);
        return Task.FromResult(vista);
    }
}

public class SalidaArchivoTxt : ISalidaDocumento
{
    private readonly string _carpetaBase;
    public string NombreMedio => "ARCHIVO_TXT";

    public SalidaArchivoTxt(string carpetaBase = "Comprobantes")
    {
        _carpetaBase = carpetaBase;
        Directory.CreateDirectory(_carpetaBase);
    }

    public async Task<string> EmitirAsync(string titulo, string contenido, CancellationToken ct = default)
    {
        string nombreArchivo = $"{titulo.Replace(' ', '_')}_{DateTime.Now:yyyyMMddHHmmss}.txt";
        string ruta = Path.Combine(_carpetaBase, nombreArchivo);
        await File.WriteAllTextAsync(ruta, $"{titulo}\n{new string('=', 40)}\n{contenido}", ct);
        return ruta;
    }
}

public class SalidaDocumentoFactory : ISalidaDocumentoFactory
{
    private readonly string _carpetaBase;

    public SalidaDocumentoFactory(string carpetaBase = "Comprobantes")
    {
        _carpetaBase = carpetaBase;
    }

    public IReadOnlyList<string> MediosDisponibles => new[] { "VISTA_PREVIA", "ARCHIVO_TXT" };


    public ISalidaDocumento Crear(string medio) => medio.Trim().ToUpperInvariant() switch
    {
        "VISTA_PREVIA" => new SalidaVistaPreviaTxt(),
        "ARCHIVO_TXT" => new SalidaArchivoTxt(_carpetaBase),
        _ => throw new ArgumentOutOfRangeException(nameof(medio), $"Medio de salida no soportado: {medio}")
    };
}
