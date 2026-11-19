namespace LogiMarket360.Application.Patterns.Singleton;

public class MetricasSesion
{
    private static readonly Lazy<MetricasSesion> _instancia = new(() => new MetricasSesion());
    public static MetricasSesion Instancia => _instancia.Value;

    private readonly object _lock = new();

    public int TotalPedidosConfirmados { get; private set; }
    public decimal MontoTotalVentas { get; private set; }
    public int TotalDevolucionesAceptadas { get; private set; }
    public decimal MontoTotalReembolsos { get; private set; }
    public int IntentosCobroFallidos { get; private set; }
    public DateTime InicioSesion { get; }

    public MetricasSesion()
    {
        InicioSesion = DateTime.UtcNow;
    }

    public void RegistrarVenta(decimal totalVenta)
    {
        lock (_lock)
        {
            TotalPedidosConfirmados++;
            MontoTotalVentas += totalVenta;
            MontoTotalVentas = decimal.Round(MontoTotalVentas, 2, MidpointRounding.AwayFromZero);
        }
    }

    public void RegistrarDevolucion(decimal montoReembolso)
    {
        lock (_lock)
        {
            TotalDevolucionesAceptadas++;
            MontoTotalReembolsos += montoReembolso;
            MontoTotalReembolsos = decimal.Round(MontoTotalReembolsos, 2, MidpointRounding.AwayFromZero);
        }
    }

    public void RegistrarCobroFallido()
    {
        lock (_lock)
        {
            IntentosCobroFallidos++;
        }
    }

    public void Reiniciar()
    {
        lock (_lock)
        {
            TotalPedidosConfirmados = 0;
            MontoTotalVentas = 0m;
            TotalDevolucionesAceptadas = 0;
            MontoTotalReembolsos = 0m;
            IntentosCobroFallidos = 0;
        }
    }
}
