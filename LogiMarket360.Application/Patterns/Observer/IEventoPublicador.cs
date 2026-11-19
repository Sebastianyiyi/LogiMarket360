namespace LogiMarket360.Application.Patterns.Observer;

public interface IEventoSubscriptor<in TEvento>
{
    Task ManejarAsync(TEvento evento, CancellationToken ct = default);
}

public interface IEventoPublicador
{
    void Suscribir<TEvento>(IEventoSubscriptor<TEvento> subscriptor);
    Task PublicarAsync<TEvento>(TEvento evento, CancellationToken ct = default);
}
