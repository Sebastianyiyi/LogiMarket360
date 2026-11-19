namespace LogiMarket360.Domain.Entities;

public class Kit
{
    public int Id { get; set; }
    public string CodigoKit { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;
    public string Descripcion { get; private set; } = string.Empty;
    public bool Activo { get; private set; } = true;

    private readonly List<KitComponente> _componentes = new();
    public virtual IReadOnlyCollection<KitComponente> Componentes => _componentes.AsReadOnly();

    protected Kit() { }

    public Kit(string codigoKit, string nombre, string descripcion = "")
    {
        if (string.IsNullOrWhiteSpace(codigoKit))
            throw new ArgumentException("El código del kit no puede ser vacío.", nameof(codigoKit));
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre no puede ser vacío.", nameof(nombre));

        CodigoKit = codigoKit.Trim().ToUpperInvariant();
        Nombre = nombre.Trim();
        Descripcion = descripcion.Trim();
        Activo = true;
    }

    public void AgregarComponente(string tipoElemento, string elementoCodigo, int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor a cero.");

        _componentes.Add(new KitComponente(Id, tipoElemento, elementoCodigo, cantidad));
    }
}
