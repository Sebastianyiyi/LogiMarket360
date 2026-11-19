namespace LogiMarket360.Application.Patterns.ChainOfResponsibility;

using LogiMarket360.Domain.Entities;

public interface IValidationStep
{
    IValidationStep SetNext(IValidationStep next);
    Task<ValidationResult> ValidateAsync(Pedido pedido, CancellationToken ct = default);
}

public abstract class ValidationStepBase : IValidationStep
{
    private IValidationStep? _next;

    public IValidationStep SetNext(IValidationStep next)
    {
        _next = next;
        return next;
    }

    public async Task<ValidationResult> ValidateAsync(Pedido pedido, CancellationToken ct = default)
    {
        var result = await ExecuteStepAsync(pedido, ct);
        if (!result.EsValido)
        {
            return result; // Detiene la cadena en el primer error
        }

        if (_next != null)
        {
            return await _next.ValidateAsync(pedido, ct);
        }

        return ValidationResult.Valido();
    }

    protected abstract Task<ValidationResult> ExecuteStepAsync(Pedido pedido, CancellationToken ct);
}
