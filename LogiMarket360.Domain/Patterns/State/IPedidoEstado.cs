namespace LogiMarket360.Domain.Patterns.State;

using LogiMarket360.Domain.Entities;
using LogiMarket360.Domain.Enums;

public interface IPedidoEstado
{
    EstadoPedido CodigoEstado { get; }
    void Validar(Pedido pedido);
    void Pagar(Pedido pedido);
    void Despachar(Pedido pedido);
    void Entregar(Pedido pedido);
    void Cancelar(Pedido pedido);
}
