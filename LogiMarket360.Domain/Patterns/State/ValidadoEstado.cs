namespace LogiMarket360.Domain.Patterns.State;

using LogiMarket360.Domain.Entities;
using LogiMarket360.Domain.Enums;
using LogiMarket360.Domain.Exceptions;

public class ValidadoEstado : IPedidoEstado
{
    public EstadoPedido CodigoEstado => EstadoPedido.VALIDADO;

    public void Validar(Pedido pedido)
    {
        throw new TransicionEstadoInvalidaException(CodigoEstado, "Validar (el pedido ya se encuentra validado)");
    }

    public void Pagar(Pedido pedido)
    {
        pedido.CambiarEstado(new PagadoEstado());
    }

    public void Despachar(Pedido pedido)
    {
        throw new TransicionEstadoInvalidaException(CodigoEstado, "Despachar (el pedido debe pagarse antes del despacho)");
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
