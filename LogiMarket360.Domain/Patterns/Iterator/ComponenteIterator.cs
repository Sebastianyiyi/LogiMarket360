namespace LogiMarket360.Domain.Patterns.Iterator;

using LogiMarket360.Domain.Patterns.Composite;

public class ComponenteIterator : IComponenteIterator
{
    private readonly List<ComponenteDesglosado> _elementos;
    private int _posicion = 0;

    public ComponenteIterator(IElementoCatalogo elementoRaiz, int cantidadInicial = 1)
    {
        ArgumentNullException.ThrowIfNull(elementoRaiz);
        if (cantidadInicial <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidadInicial), "La cantidad debe ser mayor a cero.");

        _elementos = elementoRaiz.DesglosarComponentes(cantidadInicial).ToList();
    }

    public bool HasNext()
    {
        return _posicion < _elementos.Count;
    }

    public ComponenteDesglosado Next()
    {
        if (!HasNext())
        {
            throw new InvalidOperationException("No hay más elementos en la iteración.");
        }

        var actual = _elementos[_posicion];
        _posicion++;
        return actual;
    }

    public void Reset()
    {
        _posicion = 0;
    }
}
