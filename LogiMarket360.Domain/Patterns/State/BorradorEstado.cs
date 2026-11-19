namespace LogiMarket360.Domain.Patterns.State;

using LogiMarket360.Domain.Entities;
using LogiMarket360.Domain.Enums;
using LogiMarket360.Domain.Exceptions;

public class BorradorEstado : IPedidoEstado
{
    public EstadoPedido CodigoEstado => EstadoPedido.BORRADOR;

    public void Validar(Pedido pedido)
    {
        pedido.CambiarEstado(new ValidadoEstado());
    }

    public void Pagar(Pedido pedido)
    {
        throw new TransicionEstadoInvalidaException(CodigoEstado, "Pagar (el pedido debe ser validado previamente)");
    }

    public void Despachar(Pedido pedido)
    {
        throw new TransicionEstadoInvalidaException(CodigoEstado, "Despachar");
    }

    public void Entregar(Pedido pedido)
    {
        throw new TransicionEstadoInvalidaException(CodigoEstado, "Entregar");
    }

    public void Cancelar(Pedido pedido)
    {
        pedido.CambiarEstado(new CanceladoEstado());
    }
}
