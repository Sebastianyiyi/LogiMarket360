namespace LogiMarket360.Application.Patterns.ChainOfResponsibility;

using LogiMarket360.Domain.Entities;
using LogiMarket360.Domain.Enums;

public class CondicionConfirmacionValidationStep : ValidationStepBase
{
    protected override Task<ValidationResult> ExecuteStepAsync(Pedido pedido, CancellationToken ct)
    {
        if (pedido.EstadoCodigo != EstadoPedido.BORRADOR && pedido.EstadoCodigo != EstadoPedido.VALIDADO)
        {
            return Task.FromResult(ValidationResult.Invalido("CondicionConfirmacion", 
                $"El pedido se encuentra en estado '{pedido.EstadoCodigo}' y no puede confirmarse."));
        }

        if (pedido.Total <= 0)
        {
            return Task.FromResult(ValidationResult.Invalido("CondicionConfirmacion", 
                "El monto total del pedido debe ser mayor a cero."));
        }

        return Task.FromResult(ValidationResult.Valido());
    }
}
