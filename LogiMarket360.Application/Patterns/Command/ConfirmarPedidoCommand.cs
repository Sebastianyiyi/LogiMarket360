namespace LogiMarket360.Application.Patterns.Command;

using LogiMarket360.Application.Facade;

public class ConfirmarPedidoCommand : ICommand
{
    private readonly int _pedidoId;
    private readonly string _idempotencyKey;
    private readonly ConfirmacionPedidoFacade _facade;

    public string Nombre => "ConfirmarPedidoCommand";

    public ConfirmarPedidoCommand(int pedidoId, string idempotencyKey, ConfirmacionPedidoFacade facade)
    {
        _pedidoId = pedidoId;
        _idempotencyKey = idempotencyKey;
        _facade = facade ?? throw new ArgumentNullException(nameof(facade));
    }

    public async Task<CommandResult> ExecuteAsync(CancellationToken ct = default)
    {
        var resultado = await _facade.ConfirmarAsync(_pedidoId, _idempotencyKey, ct);

        return resultado.Exitoso
            ? CommandResult.Ok(resultado.Mensaje, resultado)
            : CommandResult.Falla(resultado.Mensaje);
    }
}
