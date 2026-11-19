namespace LogiMarket360.Application.Ports.Repositories;

using LogiMarket360.Domain.Entities;
using LogiMarket360.Domain.Enums;

public interface IPedidoRepository
{
    Task<Pedido?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<Pedido?> ObtenerPorNumeroAsync(string numeroPedido, CancellationToken ct = default);
    Task<Pedido?> ObtenerPorIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default);
    Task<IReadOnlyList<Pedido>> ListarTodosAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Pedido>> ListarPorEstadoAsync(EstadoPedido estado, CancellationToken ct = default);
    Task AgregarAsync(Pedido pedido, CancellationToken ct = default);
    void Actualizar(Pedido pedido);
}
