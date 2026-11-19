namespace LogiMarket360.Application.Patterns.ChainOfResponsibility;

using LogiMarket360.Domain.Entities;

public class LineasYCantidadesValidationStep : ValidationStepBase
{
    protected override Task<ValidationResult> ExecuteStepAsync(Pedido pedido, CancellationToken ct)
    {
        if (pedido.Detalles.Count == 0)
        {
            return Task.FromResult(ValidationResult.Invalido("LineasYCantidades", "El pedido no contiene ninguna línea de compra."));
        }

        foreach (var detalle in pedido.Detalles)
        {
            if (detalle.Cantidad <= 0)
            {
                return Task.FromResult(ValidationResult.Invalido("LineasYCantidades", $"La línea '{detalle.CodigoItem}' tiene una cantidad no válida ({detalle.Cantidad})."));
            }

            if (detalle.PrecioUnitarioSnapshot <= 0)
            {
                return Task.FromResult(ValidationResult.Invalido("LineasYCantidades", $"La línea '{detalle.CodigoItem}' tiene un precio no válido ({detalle.PrecioUnitarioSnapshot})."));
            }
        }

        return Task.FromResult(ValidationResult.Valido());
    }
}
