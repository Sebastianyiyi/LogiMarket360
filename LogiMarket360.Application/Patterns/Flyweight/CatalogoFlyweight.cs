namespace LogiMarket360.Application.Patterns.Flyweight;

public sealed class MetadatosSku
{
    public string Sku { get; }
    public string Nombre { get; }
    public string Categoria { get; }
    public string Descripcion { get; }

    public MetadatosSku(string sku, string nombre, string categoria, string descripcion)
    {
        Sku = sku;
        Nombre = nombre;
        Categoria = categoria;
        Descripcion = descripcion;
    }
}

public class FilaCatalogoVista
{
    public MetadatosSku Metadatos { get; }
    public int Stock { get; set; }
    public decimal Precio { get; set; }
    public int CantidadSeleccionada { get; set; }

    public FilaCatalogoVista(MetadatosSku metadatos, int stock, decimal precio)
    {
        Metadatos = metadatos ?? throw new ArgumentNullException(nameof(metadatos));
        Stock = stock;
        Precio = precio;
    }
}

public class CatalogoVisualFlyweightFactory
{
    private readonly Dictionary<string, MetadatosSku> _cache = new(StringComparer.OrdinalIgnoreCase);
    private int _totalInstanciasCreadas;

    public int TotalInstanciasEnCache => _cache.Count;
    public int TotalInstanciasCreadas => _totalInstanciasCreadas;

    public MetadatosSku ObtenerOCrear(string sku, string nombre, string categoria, string descripcion)
    {
        if (!_cache.TryGetValue(sku, out var metadatos))
        {
            metadatos = new MetadatosSku(sku, nombre, categoria, descripcion);
            _cache[sku] = metadatos;
            _totalInstanciasCreadas++;
        }

        return metadatos;
    }

    public IReadOnlyList<FilaCatalogoVista> GenerarFilasVista(
        IEnumerable<(string Sku, string Nombre, string Categoria, string Descripcion, int Stock, decimal Precio)> datos)
    {
        return datos.Select(d =>
        {
            var metadatos = ObtenerOCrear(d.Sku, d.Nombre, d.Categoria, d.Descripcion);
            return new FilaCatalogoVista(metadatos, d.Stock, d.Precio);
        }).ToList();
    }
}
