namespace LogiMarket360.Domain.Entities;

public class Auditoria
{
    public int Id { get; set; }
    public string Usuario { get; private set; } = string.Empty;
    public string Operacion { get; private set; } = string.Empty;
    public string Entidad { get; private set; } = string.Empty;
    public string EntidadId { get; private set; } = string.Empty;
    public string Detalles { get; private set; } = string.Empty;
    public DateTime Fecha { get; private set; }
    public bool Exitoso { get; private set; }

    protected Auditoria() { }

    public Auditoria(
        string usuario,
        string operacion,
        string entidad,
        string entidadId,
        string detalles,
        bool exitoso = true)
    {
        Usuario = string.IsNullOrWhiteSpace(usuario) ? "SISTEMA" : usuario.Trim();
        Operacion = operacion.Trim();
        Entidad = entidad.Trim();
        EntidadId = entidadId.Trim();
        Detalles = detalles.Trim();
        Fecha = DateTime.UtcNow;
        Exitoso = exitoso;
    }
}
