namespace LogiMarket360.Domain.Patterns.State;

using LogiMarket360.Domain.Entities;
using LogiMarket360.Domain.Enums;
using LogiMarket360.Domain.Exceptions;

public class EntregadoEstado : IPedidoEstado
{
    public EstadoPedido CodigoEstado => EstadoPedido.ENTREGADO;

    public void Validar(Pedido pedido) => throw new TransicionEstadoInvalidaException(CodigoEstado, "Validar");
    public void Pagar(Pedido pedido) => throw new TransicionEstadoInvalidaException(CodigoEstado, "Pagar");
    public void Despachar(Pedido pedido) => throw new TransicionEstadoInvalidaException(CodigoEstado, "Despachar");
    public void Entregar(Pedido pedido) => throw new TransicionEstadoInvalidaException(CodigoEstado, "Entregar (el pedido ya fue entregado)");
    public void Cancelar(Pedido pedido) => throw new TransicionEstadoInvalidaException(CodigoEstado, "Cancelar (un pedido entregado no puede cancelarse; use devolución)");
}
