using LogiMarket360.Application.Patterns.Mediator;

namespace LogiMarket360.Tests;

public sealed class MediatorTests
{
    [Fact]
    public async Task Confirmacion_ActualizaCatalogoPedidosYMetricas_SinNotificarOrigen()
    {
        var mediator = new FormularioPrincipalMediator();
        var carrito = new VistaFalsa(AreaVista.Carrito);
        var catalogo = new VistaFalsa(AreaVista.Catalogo);
        var pedidos = new VistaFalsa(AreaVista.Pedidos);
        var metricas = new VistaFalsa(AreaVista.Metricas);
        mediator.Registrar(carrito);
        mediator.Registrar(catalogo);
        mediator.Registrar(pedidos);
        mediator.Registrar(metricas);

        await mediator.NotificarAsync(carrito, new CambioAplicacion(
            "PedidoConfirmado",
            new[] { AreaVista.Catalogo, AreaVista.Pedidos, AreaVista.Metricas }));

        Assert.Empty(carrito.Cambios);
        Assert.Single(catalogo.Cambios);
        Assert.Single(pedidos.Cambios);
        Assert.Single(metricas.Cambios);
    }

    [Fact]
    public async Task ComponenteDesregistrado_NoRecibeActualizaciones()
    {
        var mediator = new FormularioPrincipalMediator();
        var origen = new VistaFalsa(AreaVista.Carrito);
        var pedidos = new VistaFalsa(AreaVista.Pedidos);
        mediator.Registrar(origen);
        mediator.Registrar(pedidos);
        mediator.Desregistrar(pedidos);

        await mediator.NotificarAsync(origen, new CambioAplicacion(
            "PedidoCancelado",
            new[] { AreaVista.Pedidos }));

        Assert.Empty(pedidos.Cambios);
    }

    private sealed class VistaFalsa : IComponenteVista
    {
        public VistaFalsa(AreaVista area) => Area = area;
        public AreaVista Area { get; }
        public List<CambioAplicacion> Cambios { get; } = new();

        public Task ActualizarAsync(CambioAplicacion cambio, CancellationToken ct = default)
        {
            Cambios.Add(cambio);
            return Task.CompletedTask;
        }
    }
}
