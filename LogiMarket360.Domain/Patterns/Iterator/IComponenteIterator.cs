namespace LogiMarket360.Domain.Patterns.Iterator;

using LogiMarket360.Domain.Patterns.Composite;

public interface IComponenteIterator
{
    bool HasNext();
    ComponenteDesglosado Next();
    void Reset();
}
