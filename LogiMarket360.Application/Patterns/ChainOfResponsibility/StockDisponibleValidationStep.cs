namespace LogiMarket360.Application.Patterns.ChainOfResponsibility;

using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Domain.Entities;

public class StockDisponibleValidationStep : ValidationStepBase
{
    private readonly IProductoRepository _productoRepository;

    public StockDisponibleValidationStep(IProductoRepository productoRepository)
    {
        _productoRepository = productoRepository ?? throw new ArgumentNullException(nameof(productoRepository));
    }

    protected override async Task<ValidationResult> ExecuteStepAsync(Pedido pedido, CancellationToken ct)
    {
        // Consolidar todas las unidades físicas finales por SKU
        var requerimientos = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var detalle in pedido.Detalles)
        {
            if (detalle.ComponentesSnapshot.Count > 0)
            {
                foreach (var comp in detalle.ComponentesSnapshot)
                {
                    requerimientos[comp.Sku] = requerimientos.GetValueOrDefault(comp.Sku) + comp.CantidadFisica;
                }
            }
            else
            {
                requerimientos[detalle.CodigoItem] = requerimientos.GetValueOrDefault(detalle.CodigoItem) + detalle.Cantidad;
            }
        }

        foreach (var (sku, cantidadRequerida) in requerimientos)
        {
            var producto = await _productoRepository.ObtenerPorSkuAsync(sku, ct);
            if (producto == null)
            {
                return ValidationResult.Invalido("StockDisponible", $"El producto con SKU '{sku}' no existe en el catálogo.");
            }

            if (producto.Stock < cantidadRequerida)
            {
                return ValidationResult.Invalido("StockDisponible", 
                    $"Stock insuficiente para '{sku}' ({producto.Nombre}). Disponible: {producto.Stock}, Requerido: {cantidadRequerida}.");
            }
        }

        return ValidationResult.Valido();
    }
}
