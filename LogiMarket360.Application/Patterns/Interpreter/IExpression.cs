namespace LogiMarket360.Application.Patterns.Interpreter;

using LogiMarket360.Domain.Entities;
using LogiMarket360.Domain.Enums;

public record ContextoPedido(
    Pedido Pedido,
    string NombreCliente
);

public interface IExpression
{
    bool Interpretar(ContextoPedido contexto);
}
