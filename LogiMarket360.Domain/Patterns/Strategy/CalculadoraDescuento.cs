namespace LogiMarket360.Domain.Patterns.Strategy;

public class CalculadoraDescuento
{
    private readonly List<IDescuentoStrategy> _estrategias;

    public CalculadoraDescuento(IEnumerable<IDescuentoStrategy>? estrategias = null)
    {
        if (estrategias != null && estrategias.Any())
        {
            _estrategias = estrategias.ToList();
        }
        else
        {
            // Estrategias por defecto del dominio
            _estrategias = new List<IDescuentoStrategy>
            {
                new Mayorista15Strategy(),
                new Vip10Strategy(),
                new SinDescuentoStrategy()
            };
        }
    }

    public void AgregarEstrategia(IDescuentoStrategy estrategia)
    {
        ArgumentNullException.ThrowIfNull(estrategia);
        _estrategias.Add(estrategia);
    }

    public (string NombreEstrategia, decimal MontoDescuento) Calcular(ContextoDescuento contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        // Selecciona la estrategia aplicable con mayor prioridad (menor valor numérico)
        var estrategiaSeleccionada = _estrategias
            .Where(e => e.Aplica(contexto))
            .OrderBy(e => e.Prioridad)
            .First();

        decimal descuento = estrategiaSeleccionada.CalcularDescuento(contexto);
        return (estrategiaSeleccionada.Nombre, descuento);
    }
}
