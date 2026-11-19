namespace LogiMarket360.Domain.Entities;

public class EventoOutbox
{
    public Guid Id { get; private set; }
    public string TipoEvento { get; private set; } = string.Empty;
    public string ContenidoJson { get; private set; } = string.Empty;
    public DateTime FechaCreacion { get; private set; }
    public bool Procesado { get; private set; }
    public DateTime? FechaProcesamiento { get; private set; }
    public int Intentos { get; private set; }
    public string? Error { get; private set; }

    protected EventoOutbox() { }

    public EventoOutbox(string tipoEvento, string contenidoJson)
    {
        Id = Guid.NewGuid();
        TipoEvento = tipoEvento.Trim();
        ContenidoJson = contenidoJson;
        FechaCreacion = DateTime.UtcNow;
        Procesado = false;
        Intentos = 0;
    }

    public void MarcarProcesado()
    {
        Procesado = true;
        FechaProcesamiento = DateTime.UtcNow;
        Error = null;
    }

    public void RegistrarFallo(string error)
    {
        Intentos++;
        Error = error;
    }
}
