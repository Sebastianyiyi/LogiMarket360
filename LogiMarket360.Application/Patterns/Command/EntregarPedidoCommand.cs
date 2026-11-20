namespace LogiMarket360.Application.Patterns.Command;

using LogiMarket360.Application.Ports;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Application.Security;
using LogiMarket360.Domain.Entities;

public class EntregarPedidoCommand : ICommand
{
    private readonly int _pedidoId;
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IDespachoRepository _despachoRepository;
    private readonly IAuditoriaRepository _auditoriaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAutorizador _autorizador;

    public string Nombre => "EntregarPedidoCommand";

    public EntregarPedidoCommand(
        int pedidoId,
        IPedidoRepository pedidoRepository,
        IDespachoRepository despachoRepository,
        IAuditoriaRepository auditoriaRepository,
        IUnitOfWork unitOfWork,
        IAutorizador autorizador)
    {
        _pedidoId = pedidoId;
        _pedidoRepository = pedidoRepository;
        _despachoRepository = despachoRepository;
        _auditoriaRepository = auditoriaRepository;
        _unitOfWork = unitOfWork;
        _autorizador = autorizador;
    }

    public async Task<CommandResult> ExecuteAsync(CancellationToken ct = default)
    {
        try
        {
            _autorizador.ExigirRol(Permisos.Despacho);
        }
        catch (UnauthorizedAccessException ex)
        {
            return CommandResult.Falla(ex.Message);
        }

        var pedido = await _pedidoRepository.ObtenerPorIdAsync(_pedidoId, ct);
        if (pedido is null)
            return CommandResult.Falla($"Pedido con ID '{_pedidoId}' no encontrado.");

        var despacho = await _despachoRepository.ObtenerPorPedidoIdAsync(_pedidoId, ct);
        if (despacho is null)
            return CommandResult.Falla("El pedido no tiene un despacho registrado.");

        try
        {
            pedido.Entregar();
            _pedidoRepository.Actualizar(pedido);

            despacho.MarcarEntregado();
            _despachoRepository.Actualizar(despacho);

            await _auditoriaRepository.AgregarAsync(new Auditoria(
                usuario: "BODEGA",
                operacion: "ENTREGA_PEDIDO",
                entidad: "Pedido",
                entidadId: pedido.Id.ToString(),
                detalles: $"Entrega confirmada. Guía: {despacho.NumeroGuia}",
                exitoso: true), ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return CommandResult.Ok($"Pedido {pedido.NumeroPedido} marcado como ENTREGADO.");
        }
        catch (Domain.Exceptions.DomainException ex)
        {
            return CommandResult.Falla(ex.Message);
        }
        catch (Exception ex)
        {
            return CommandResult.Falla($"Error al registrar la entrega: {ex.Message}");
        }
    }
}
