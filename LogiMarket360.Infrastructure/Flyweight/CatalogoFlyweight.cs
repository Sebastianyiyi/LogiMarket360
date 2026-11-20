namespace LogiMarket360.Infrastructure.Flyweight;

// FLYWEIGHT: metadatos inmutables de SKU compartidos entre filas de la vista
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

// Estado extrínseco por fila (NO compartido)
public class FilaCatalogoVista
{
    public MetadatosSku Metadatos { get; }   // Compartido (flyweight)
    public int Stock { get; set; }            // Estado externo por fila
    public decimal Precio { get; set; }       // Estado externo por fila
    public int CantidadSeleccionada { get; set; } // Estado externo por fila

    public FilaCatalogoVista(MetadatosSku metadatos, int stock, decimal precio)
    {
        Metadatos = metadatos ?? throw new ArgumentNullException(nameof(metadatos));
        Stock = stock;
        Precio = precio;
    }
}

// CatalogoVisualFlyweightFactory: crea y comparte metadatos inmutables
public class CatalogoVisualFlyweightFactory
{
    private readonly Dictionary<string, MetadatosSku> _cache = new(StringComparer.OrdinalIgnoreCase);
    private int _totalInstanciasCreadas = 0;

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

        return metadatos; // Retorna la misma instancia si ya existe (ReferenceEquals = true)
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
