namespace LogiMarket360.Domain.Patterns.Composite;

using LogiMarket360.Domain.Exceptions;
using LogiMarket360.Domain.Patterns.Visitor;

public class KitCompuesto : IElementoCatalogo
{
    private readonly List<(IElementoCatalogo Elemento, int Cantidad)> _componentes = new();

    public string Codigo => CodigoKit;
    public string CodigoKit { get; }
    public string Nombre { get; }
    public IReadOnlyList<(IElementoCatalogo Elemento, int Cantidad)> Componentes => _componentes.AsReadOnly();

    public KitCompuesto(string codigoKit, string nombre)
    {
        if (string.IsNullOrWhiteSpace(codigoKit))
            throw new ArgumentException("El código del kit no puede ser vacío.", nameof(codigoKit));
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre no puede ser vacío.", nameof(nombre));

        CodigoKit = codigoKit.Trim().ToUpperInvariant();
        Nombre = nombre.Trim();
    }

    public void AgregarComponente(IElementoCatalogo elemento, int cantidad = 1)
    {
        ArgumentNullException.ThrowIfNull(elemento);
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor a cero.");

        // Detección de referencia circular directa
        if (string.Equals(elemento.Codigo, CodigoKit, StringComparison.OrdinalIgnoreCase))
        {
            throw new CicloDetectadoException(CodigoKit, $"El kit no puede contenerse a sí mismo directamente.");
        }

        // Detección de referencia circular indirecta (si el elemento ya contiene este kit)
        if (elemento.ContieneElemento(CodigoKit))
        {
            throw new CicloDetectadoException(CodigoKit, $"El kit '{elemento.Codigo}' ya contiene al kit '{CodigoKit}', creando una referencia circular.");
        }

        _componentes.Add((elemento, cantidad));
    }

    public decimal ObtenerPrecio()
    {
        decimal total = 0m;
        foreach (var (elemento, cantidad) in _componentes)
        {
            total += elemento.ObtenerPrecio() * cantidad;
        }
        return decimal.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    public decimal ObtenerPeso()
    {
        decimal total = 0m;
        foreach (var (elemento, cantidad) in _componentes)
        {
            total += elemento.ObtenerPeso() * cantidad;
        }
        return decimal.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    public IReadOnlyList<ComponenteDesglosado> DesglosarComponentes(int factor = 1)
    {
        if (factor <= 0)
            throw new ArgumentOutOfRangeException(nameof(factor), "El factor debe ser mayor a cero.");

        var resultado = new List<ComponenteDesglosado>();

        foreach (var (elemento, cantidad) in _componentes)
        {
            int cantidadAcumulada = cantidad * factor;
            var subComponentes = elemento.DesglosarComponentes(cantidadAcumulada);
            resultado.AddRange(subComponentes);
        }

        // Agrupar por SKU para consolidar cantidades si un mismo SKU aparece varias veces
        return resultado
            .GroupBy(c => c.Sku, StringComparer.OrdinalIgnoreCase)
            .Select(g => new ComponenteDesglosado(
                Sku: g.Key,
                Nombre: g.First().Nombre,
                PrecioUnitario: g.First().PrecioUnitario,
                PesoKg: g.First().PesoKg,
                Cantidad: g.Sum(x => x.Cantidad)
            ))
            .ToList();
    }

    public void Aceptar(ICatalogoVisitor visitor, int cantidad = 1)
    {
        visitor.VisitarKit(this, cantidad);
        foreach (var (elemento, cant) in _componentes)
        {
            elemento.Aceptar(visitor, cant * cantidad);
        }
    }

    public bool ContieneElemento(string codigo)
    {
        if (string.Equals(CodigoKit, codigo, StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (var (elemento, _) in _componentes)
        {
            if (elemento.ContieneElemento(codigo))
                return true;
        }

        return false;
    }
}
