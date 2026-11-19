namespace LogiMarket360.Application.Patterns.ChainOfResponsibility;

public record ValidationResult(
    bool EsValido,
    string? PasoFallido = null,
    string? MensajeError = null
)
{
    public static ValidationResult Valido() => new(true);
    public static ValidationResult Invalido(string paso, string mensaje) => new(false, paso, mensaje);
}
