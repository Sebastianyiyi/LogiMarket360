namespace LogiMarket360.Application.Ports;

public interface IOutboxProcesador
{
    Task<int> ProcesarPendientesAsync(int limite = 50, CancellationToken ct = default);
}
