namespace LogiMarket360.Domain.Patterns.Composite;

using LogiMarket360.Domain.Patterns.Visitor;

public interface IElementoCatalogo
{
    string Codigo { get; }
    string Nombre { get; }
    decimal ObtenerPrecio();
    decimal ObtenerPeso();
    IReadOnlyList<ComponenteDesglosado> DesglosarComponentes(int factor = 1);
    void Aceptar(ICatalogoVisitor visitor, int cantidad = 1);
    bool ContieneElemento(string codigo);
}
