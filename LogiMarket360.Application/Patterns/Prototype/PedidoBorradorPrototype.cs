namespace LogiMarket360.Application.Patterns.Prototype;

using LogiMarket360.Domain.Entities;

public interface IPrototipo<T>
{
    T Clonar();
}

public class PedidoBorradorPrototype : IPrototipo<Pedido>
{
    private readonly Pedido _pedidoPlantilla;

    public PedidoBorradorPrototype(Pedido pedidoPlantilla)
    {
        ArgumentNullException.ThrowIfNull(pedidoPlantilla);
        _pedidoPlantilla = pedidoPlantilla;
    }

    public Pedido Clonar()
    {
        // Se crea un nuevo pedido borrador independiente sin id de persistencia ni estado de pago
        string nuevoNumero = $"PED-CLON-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        string nuevaClaveIdempotencia = Guid.NewGuid().ToString();

        var clon = new Pedido(
            nuevoNumero,
            _pedidoPlantilla.ClienteId,
            _pedidoPlantilla.TipoEntrega,
            _pedidoPlantilla.CostoEnvio,
            nuevaClaveIdempotencia
        );

        // Clonación profunda de detalles y colecciones anidadas
        foreach (var detalleOriginal in _pedidoPlantilla.Detalles)
        {
            var componentesClonados = detalleOriginal.ComponentesSnapshot.Select(c => new PedidoDetalleComponente(
                sku: c.Sku,
                nombre: c.Nombre,
                cantidadFisica: c.CantidadFisica,
                precioUnitarioSnapshot: c.PrecioUnitarioSnapshot,
                pesoKgSnapshot: c.PesoKgSnapshot
            )).ToList();

            var detalleClonado = new PedidoDetalle(
                codigoItem: detalleOriginal.CodigoItem,
                nombreItem: detalleOriginal.NombreItem,
                esKit: detalleOriginal.EsKit,
                cantidad: detalleOriginal.Cantidad,
                precioUnitarioSnapshot: detalleOriginal.PrecioUnitarioSnapshot,
                componentes: componentesClonados
            );

            clon.AgregarDetalle(detalleClonado);
        }

        clon.RecalcularTotales(_pedidoPlantilla.Descuento, _pedidoPlantilla.EstrategiaDescuento);
        return clon;
    }
}
