using LogiMarket360.Domain.Enums;

namespace LogiMarket360.Domain.Exceptions;

public class TransicionEstadoInvalidaException : DomainException
{
    public EstadoPedido EstadoActual { get; }
    public string AccionIntentada { get; }

    public TransicionEstadoInvalidaException(EstadoPedido estadoActual, string accionIntentada)
        : base($"No se permite la acción '{accionIntentada}' sobre un pedido en estado '{estadoActual}'.")
    {
        EstadoActual = estadoActual;
        AccionIntentada = accionIntentada;
    }
}
