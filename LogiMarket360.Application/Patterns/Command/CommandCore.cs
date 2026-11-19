namespace LogiMarket360.Application.Patterns.Command;

public record CommandResult(
    bool Exitoso,
    string Mensaje,
    object? Datos = null
)
{
    public static CommandResult Ok(string mensaje, object? datos = null) => new(true, mensaje, datos);
    public static CommandResult Falla(string mensaje) => new(false, mensaje);
}

public interface ICommand
{
    string Nombre { get; }
    Task<CommandResult> ExecuteAsync(CancellationToken ct = default);
}

public record RegistroInvocacion(
    string NombreComando,
    DateTime Timestamp,
    bool Exitoso,
    string Mensaje
);

public interface ICommandInvoker
{
    IReadOnlyList<RegistroInvocacion> Historial { get; }
    Task<CommandResult> InvocarAsync(ICommand comando, CancellationToken ct = default);
}

public class CommandInvoker : ICommandInvoker
{
    private readonly List<RegistroInvocacion> _historial = new();
    public IReadOnlyList<RegistroInvocacion> Historial => _historial.AsReadOnly();

    public async Task<CommandResult> InvocarAsync(ICommand comando, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        try
        {
            var resultado = await comando.ExecuteAsync(ct);
            _historial.Add(new RegistroInvocacion(
                comando.Nombre,
                DateTime.UtcNow,
                resultado.Exitoso,
                resultado.Mensaje
            ));
            return resultado;
        }
        catch (Exception ex)
        {
            _historial.Add(new RegistroInvocacion(
                comando.Nombre,
                DateTime.UtcNow,
                false,
                $"Excepción: {ex.Message}"
            ));
            return CommandResult.Falla($"Error ejecutando {comando.Nombre}: {ex.Message}");
        }
    }
}
