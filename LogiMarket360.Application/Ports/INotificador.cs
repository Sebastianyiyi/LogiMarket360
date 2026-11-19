namespace LogiMarket360.Application.Ports;

public interface INotificador
{
    string Canal { get; }
    Task<bool> EnviarAsync(string destinatario, string mensaje, CancellationToken ct = default);
}
