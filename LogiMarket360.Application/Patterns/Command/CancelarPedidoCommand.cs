namespace LogiMarket360.Application.Patterns.Command;

using LogiMarket360.Application.Ports;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Application.Security;
using LogiMarket360.Domain.Entities;

public class CancelarPedidoCommand : ICommand
{
    private readonly int _pedidoId;
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaRepository _auditoriaRepository;
    private readonly IAutorizador _autorizador;

    public string Nombre => "CancelarPedidoCommand";

    public CancelarPedidoCommand(
        int pedidoId,
        IPedidoRepository pedidoRepository,
        IUnitOfWork unitOfWork,
        IAuditoriaRepository auditoriaRepository,
        IAutorizador autorizador)
    {
        _pedidoId = pedidoId;
        _pedidoRepository = pedidoRepository;
        _unitOfWork = unitOfWork;
        _auditoriaRepository = auditoriaRepository;
        _autorizador = autorizador;
    }

    public async Task<CommandResult> ExecuteAsync(CancellationToken ct = default)
    {
        try
        {
            _autorizador.ExigirRol(Permisos.Ventas);
        }
        catch (UnauthorizedAccessException ex)
        {
            return CommandResult.Falla(ex.Message);
        }

        var pedido = await _pedidoRepository.ObtenerPorIdAsync(_pedidoId, ct);
        if (pedido == null)
        {
            return CommandResult.Falla($"No se encontró el pedido con ID '{_pedidoId}'.");
        }

        try
        {
            pedido.Cancelar();
            _pedidoRepository.Actualizar(pedido);

            var audit = new Auditoria(
                usuario: "SISTEMA",
                operacion: "CANCELACION_PEDIDO",
                entidad: "Pedido",
                entidadId: pedido.Id.ToString(),
                detalles: $"Pedido {pedido.NumeroPedido} cancelado.",
                exitoso: true
            );
            await _auditoriaRepository.AgregarAsync(audit, ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return CommandResult.Ok($"Pedido {pedido.NumeroPedido} cancelado exitosamente.");
        }
        catch (Exception ex)
        {
            return CommandResult.Falla($"No se pudo cancelar el pedido: {ex.Message}");
        }
    }
}
