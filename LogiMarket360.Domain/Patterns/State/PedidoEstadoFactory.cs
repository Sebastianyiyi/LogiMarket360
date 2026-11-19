namespace LogiMarket360.Domain.Patterns.State;

using LogiMarket360.Domain.Enums;

public static class PedidoEstadoFactory
{
    public static IPedidoEstado CrearEstado(EstadoPedido estado)
    {
        return estado switch
        {
            EstadoPedido.BORRADOR => new BorradorEstado(),
            EstadoPedido.VALIDADO => new ValidadoEstado(),
            EstadoPedido.PAGADO => new PagadoEstado(),
            EstadoPedido.DESPACHADO => new DespachadoEstado(),
            EstadoPedido.ENTREGADO => new EntregadoEstado(),
            EstadoPedido.CANCELADO => new CanceladoEstado(),
            _ => throw new ArgumentOutOfRangeException(nameof(estado), $"Estado de pedido no soportado: {estado}")
        };
    }
}
