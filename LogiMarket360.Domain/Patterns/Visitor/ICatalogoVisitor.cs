namespace LogiMarket360.Domain.Patterns.Visitor;

using LogiMarket360.Domain.Patterns.Composite;

public interface ICatalogoVisitor
{
    void VisitarProducto(ProductoSimple producto, int cantidad);
    void VisitarKit(KitCompuesto kit, int cantidad);
}
