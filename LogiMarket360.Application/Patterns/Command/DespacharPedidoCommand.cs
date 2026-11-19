namespace LogiMarket360.Application.Patterns.Command;

using LogiMarket360.Application.Patterns.Observer;
using LogiMarket360.Application.Patterns.Observer.Eventos;
using LogiMarket360.Application.Ports;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Domain.Entities;

public class DespacharPedidoCommand : ICommand
{
    private readonly int _pedidoId;
    private readonly string _numeroGuia;
    private readonly string _proveedorLogistico;
    private readonly int _plazoDias;
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventoPublicador _eventoPublicador;

    public string Nombre => "DespacharPedidoCommand";

    public DespacharPedidoCommand(
        int pedidoId,
        string numeroGuia,
        string proveedorLogistico,
        int plazoDias,
        IPedidoRepository pedidoRepository,
        IUnitOfWork unitOfWork,
        IEventoPublicador eventoPublicador)
    {
        _pedidoId = pedidoId;
        _numeroGuia = numeroGuia;
        _proveedorLogistico = proveedorLogistico;
        _plazoDias = plazoDias;
        _pedidoRepository = pedidoRepository;
        _unitOfWork = unitOfWork;
        _eventoPublicador = eventoPublicador;
    }

    public async Task<CommandResult> ExecuteAsync(CancellationToken ct = default)
    {
        var pedido = await _pedidoRepository.ObtenerPorIdAsync(_pedidoId, ct);
        if (pedido == null)
            return CommandResult.Falla($"Pedido con ID '{_pedidoId}' no encontrado.");

        try
        {
            pedido.Despachar();
            _pedidoRepository.Actualizar(pedido);

            await _unitOfWork.SaveChangesAsync(ct);

            await _eventoPublicador.PublicarAsync(new DespachoRegistradoEvent(
                pedido.Id,
                _numeroGuia,
                _proveedorLogistico,
                DateTime.UtcNow
            ), ct);

            return CommandResult.Ok($"Pedido {pedido.NumeroPedido} despachado exitosamente con guía {_numeroGuia}.");
        }
        catch (Exception ex)
        {
            return CommandResult.Falla($"Error al despachar pedido: {ex.Message}");
        }
    }
}
