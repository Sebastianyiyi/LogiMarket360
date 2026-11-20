namespace LogiMarket360.Application.Ports.Repositories;

using LogiMarket360.Domain.Entities;

public interface IPagoRepository
{
    Task AgregarAsync(Pago pago, CancellationToken ct = default);
    Task<Pago?> ObtenerPorClaveIdempotenciaAsync(string claveIdempotencia, CancellationToken ct = default);
    Task<Pago?> ObtenerPorPedidoIdAsync(int pedidoId, CancellationToken ct = default);
    Task<IReadOnlyList<Pago>> ListarTodosAsync(CancellationToken ct = default);
}
