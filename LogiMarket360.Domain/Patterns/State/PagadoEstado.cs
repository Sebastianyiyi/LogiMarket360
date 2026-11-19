namespace LogiMarket360.Domain.Patterns.State;

using LogiMarket360.Domain.Entities;
using LogiMarket360.Domain.Enums;
using LogiMarket360.Domain.Exceptions;

public class PagadoEstado : IPedidoEstado
{
    public EstadoPedido CodigoEstado => EstadoPedido.PAGADO;

    public void Validar(Pedido pedido)
    {
        throw new TransicionEstadoInvalidaException(CodigoEstado, "Validar");
    }

    public void Pagar(Pedido pedido)
    {
        throw new TransicionEstadoInvalidaException(CodigoEstado, "Pagar (el pedido ya fue pagado)");
    }

    public void Despachar(Pedido pedido)
    {
        pedido.CambiarEstado(new DespachadoEstado());
    }

    public void Entregar(Pedido pedido)
    {
        throw new TransicionEstadoInvalidaException(CodigoEstado, "Entregar (el pedido debe despacharse primero)");
    }

    public void Cancelar(Pedido pedido)
    {
        throw new TransicionEstadoInvalidaException(CodigoEstado, "Cancelar (un pedido pagado no puede cancelarse directamente)");
    }
}
