namespace LogiMarket360.Domain.Patterns.Visitor;

using LogiMarket360.Domain.Patterns.Composite;

public class DesgloseSkuVisitor : ICatalogoVisitor
{
    private readonly Dictionary<string, (string Nombre, decimal PrecioUnitario, decimal PesoKg, int Cantidad)> _acumulador = new(StringComparer.OrdinalIgnoreCase);

    public void VisitarProducto(ProductoSimple producto, int cantidad)
    {
        if (_acumulador.TryGetValue(producto.Sku, out var actual))
        {
            _acumulador[producto.Sku] = (actual.Nombre, actual.PrecioUnitario, actual.PesoKg, actual.Cantidad + cantidad);
        }
        else
        {
            _acumulador[producto.Sku] = (producto.Nombre, producto.Precio, producto.PesoKg, cantidad);
        }
    }

    public void VisitarKit(KitCompuesto kit, int cantidad)
    {
        // El desglose se enfoca en los productos físicos hoja; 
        // no agrega el kit como ítem físico, pero participa en la travesía.
    }

    public IReadOnlyList<ComponenteDesglosado> ObtenerDesglose()
    {
        return _acumulador.Select(kvp => new ComponenteDesglosado(
            Sku: kvp.Key,
            Nombre: kvp.Value.Nombre,
            PrecioUnitario: kvp.Value.PrecioUnitario,
            PesoKg: kvp.Value.PesoKg,
            Cantidad: kvp.Value.Cantidad
        )).ToList();
    }
}
