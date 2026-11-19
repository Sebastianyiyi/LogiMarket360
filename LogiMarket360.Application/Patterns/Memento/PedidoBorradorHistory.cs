namespace LogiMarket360.Application.Patterns.Memento;

public class PedidoBorradorHistory
{
    private const int MaxCapacidad = 5;
    private readonly List<PedidoBorradorMemento> _deshacerStack = new();
    private readonly List<PedidoBorradorMemento> _rehacerStack = new();
    private bool _bloqueado = false;

    public bool PuedeDeshacer => !_bloqueado && _deshacerStack.Count > 1;
    public bool PuedeRehacer => !_bloqueado && _rehacerStack.Count > 0;
    public int ConteoEstados => _deshacerStack.Count;

    public void GuardarEstado(PedidoBorradorMemento memento)
    {
        if (_bloqueado)
            throw new InvalidOperationException("El historial de estados se encuentra bloqueado debido a que el pedido ya fue confirmado.");

        ArgumentNullException.ThrowIfNull(memento);

        _deshacerStack.Add(memento);
        if (_deshacerStack.Count > MaxCapacidad)
        {
            _deshacerStack.RemoveAt(0);
        }

        // Cualquier nueva modificación invalida el historial de rehacer
        _rehacerStack.Clear();
    }

    public PedidoBorradorMemento Deshacer()
    {
        if (!PuedeDeshacer)
            throw new InvalidOperationException("No hay estados previos para deshacer.");

        // El último elemento es el estado actual, moverlo a rehacer
        int indiceActual = _deshacerStack.Count - 1;
        var estadoActual = _deshacerStack[indiceActual];
        _deshacerStack.RemoveAt(indiceActual);
        _rehacerStack.Add(estadoActual);

        // Retornar el nuevo tope (estado previo)
        return _deshacerStack[^1];
    }

    public PedidoBorradorMemento Rehacer()
    {
        if (!PuedeRehacer)
            throw new InvalidOperationException("No hay estados para rehacer.");

        int indiceRehacer = _rehacerStack.Count - 1;
        var estado = _rehacerStack[indiceRehacer];
        _rehacerStack.RemoveAt(indiceRehacer);
        _deshacerStack.Add(estado);

        return estado;
    }

    public void BloquearYLimpiar()
    {
        _bloqueado = true;
        _deshacerStack.Clear();
        _rehacerStack.Clear();
    }

    public void Desbloquear()
    {
        _bloqueado = false;
        _deshacerStack.Clear();
        _rehacerStack.Clear();
    }
}
