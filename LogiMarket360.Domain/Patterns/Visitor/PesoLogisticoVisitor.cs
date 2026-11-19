namespace LogiMarket360.Domain.Patterns.Visitor;

using LogiMarket360.Domain.Patterns.Composite;

public class PesoLogisticoVisitor : ICatalogoVisitor
{
    public decimal PesoTotalKg { get; private set; } = 0m;

    public void VisitarProducto(ProductoSimple producto, int cantidad)
    {
        PesoTotalKg += producto.PesoKg * cantidad;
        PesoTotalKg = decimal.Round(PesoTotalKg, 2, MidpointRounding.AwayFromZero);
    }

    public void VisitarKit(KitCompuesto kit, int cantidad)
    {
        // El peso se acumula a través de los productos físicos contenidos
    }
}
