namespace LogiMarket360.Application.Patterns.Observer;

public class EventoPublicador : IEventoPublicador
{
    private readonly Dictionary<Type, List<object>> _subscriptores = new();
    private readonly object _lock = new();

    public void Suscribir<TEvento>(IEventoSubscriptor<TEvento> subscriptor)
    {
        ArgumentNullException.ThrowIfNull(subscriptor);
        lock (_lock)
        {
            var tipo = typeof(TEvento);
            if (!_subscriptores.TryGetValue(tipo, out var lista))
            {
                lista = new List<object>();
                _subscriptores[tipo] = lista;
            }
            lista.Add(subscriptor);
        }
    }

    public async Task PublicarAsync<TEvento>(TEvento evento, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(evento);

        List<object> subscriptoresCopia;
        lock (_lock)
        {
            if (!_subscriptores.TryGetValue(typeof(TEvento), out var lista))
                return;

            subscriptoresCopia = lista.ToList();
        }

        foreach (var subscriptor in subscriptoresCopia)
        {
            if (subscriptor is IEventoSubscriptor<TEvento> typedSubscriptor)
            {
                await typedSubscriptor.ManejarAsync(evento, ct);
            }
        }
    }
}
