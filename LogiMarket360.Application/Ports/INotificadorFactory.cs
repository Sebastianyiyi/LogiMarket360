namespace LogiMarket360.Application.Ports;

public interface INotificadorFactory
{
    INotificador Crear(string canal);
    string CanalPorDefecto { get; }
}
