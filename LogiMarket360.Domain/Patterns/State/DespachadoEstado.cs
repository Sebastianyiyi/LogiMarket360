namespace LogiMarket360.Domain.Patterns.State;

using LogiMarket360.Domain.Entities;
using LogiMarket360.Domain.Enums;
using LogiMarket360.Domain.Exceptions;

public class DespachadoEstado : IPedidoEstado
{
    public EstadoPedido CodigoEstado => EstadoPedido.DESPACHADO;

    public void Validar(Pedido pedido)
    {
        throw new TransicionEstadoInvalidaException(CodigoEstado, "Validar");
    }

    public void Pagar(Pedido pedido)
    {
        throw new TransicionEstadoInvalidaException(CodigoEstado, "Pagar");
    }

    public void Despachar(Pedido pedido)
    {
        throw new TransicionEstadoInvalidaException(CodigoEstado, "Despachar (el pedido ya fue despachado)");
    }

    public void Entregar(Pedido pedido)
    {
        pedido.CambiarEstado(new EntregadoEstado());
    }

    public void Cancelar(Pedido pedido)
    {
        throw new TransicionEstadoInvalidaException(CodigoEstado, "Cancelar");
    }
}
