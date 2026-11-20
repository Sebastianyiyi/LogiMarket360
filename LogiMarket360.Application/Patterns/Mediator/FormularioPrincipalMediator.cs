namespace LogiMarket360.Application.Patterns.Mediator;

using System.Collections.Concurrent;

public enum AreaVista
{
    Catalogo,
    Carrito,
    Pedidos,
    Metricas
}

public sealed record CambioAplicacion(
    string Operacion,
    IReadOnlyCollection<AreaVista> AreasAfectadas,
    object? Datos = null);

public interface IComponenteVista
{
    AreaVista Area { get; }
    Task ActualizarAsync(CambioAplicacion cambio, CancellationToken ct = default);
}

public interface IFormularioPrincipalMediator
{
    void Registrar(IComponenteVista componente);
    void Desregistrar(IComponenteVista componente);
    Task NotificarAsync(IComponenteVista origen, CambioAplicacion cambio, CancellationToken ct = default);
}

public sealed class FormularioPrincipalMediator : IFormularioPrincipalMediator
{
    private readonly ConcurrentDictionary<IComponenteVista, byte> _componentes = new();

    public void Registrar(IComponenteVista componente)
    {
        ArgumentNullException.ThrowIfNull(componente);
        _componentes.TryAdd(componente, 0);
    }

    public void Desregistrar(IComponenteVista componente)
    {
        ArgumentNullException.ThrowIfNull(componente);
        _componentes.TryRemove(componente, out _);
    }

    public async Task NotificarAsync(
        IComponenteVista origen,
        CambioAplicacion cambio,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(origen);
        ArgumentNullException.ThrowIfNull(cambio);

        var destinos = _componentes.Keys
            .Where(componente => !ReferenceEquals(componente, origen))
            .Where(componente => cambio.AreasAfectadas.Contains(componente.Area))
            .ToArray();

        await Task.WhenAll(destinos.Select(destino => destino.ActualizarAsync(cambio, ct)));
    }
}
