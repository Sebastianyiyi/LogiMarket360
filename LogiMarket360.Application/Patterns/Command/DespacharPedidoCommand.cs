namespace LogiMarket360.Application.Patterns.Command;

using System.Text.Json;
using LogiMarket360.Application.Exceptions;
using LogiMarket360.Application.Ports;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Application.Security;
using LogiMarket360.Domain.Entities;
using LogiMarket360.Domain.Enums;

public class DespacharPedidoCommand : ICommand
{
    private readonly int _pedidoId;
    private readonly string? _numeroGuia;
    private readonly string _proveedorLogistico;
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IDespachoRepository _despachoRepository;
    private readonly IEntregaFactoryProvider _entregaProvider;
    private readonly IEventoOutboxRepository _outboxRepository;
    private readonly IOutboxProcesador _outboxProcesador;
    private readonly IAuditoriaRepository _auditoriaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAutorizador _autorizador;

    public string Nombre => "DespacharPedidoCommand";

    public DespacharPedidoCommand(
        int pedidoId,
        string? numeroGuia,
        string proveedorLogistico,
        IPedidoRepository pedidoRepository,
        IDespachoRepository despachoRepository,
        IEntregaFactoryProvider entregaProvider,
        IEventoOutboxRepository outboxRepository,
        IOutboxProcesador outboxProcesador,
        IAuditoriaRepository auditoriaRepository,
        IUnitOfWork unitOfWork,
        IAutorizador autorizador)
    {
        _pedidoId = pedidoId;
        _numeroGuia = numeroGuia;
        _proveedorLogistico = proveedorLogistico;
        _pedidoRepository = pedidoRepository;
        _despachoRepository = despachoRepository;
        _entregaProvider = entregaProvider;
        _outboxRepository = outboxRepository;
        _outboxProcesador = outboxProcesador;
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
        if (pedido == null)
            return CommandResult.Falla($"Pedido con ID '{_pedidoId}' no encontrado.");

        try
        {
            var fabrica = _entregaProvider.Obtener(pedido.TipoEntrega);
            string guia = string.IsNullOrWhiteSpace(_numeroGuia)
                ? fabrica.CrearGeneradorGuia().GenerarGuia(pedido.NumeroPedido)
                : _numeroGuia.Trim();

            pedido.Despachar();
            _pedidoRepository.Actualizar(pedido);

            var despacho = new Despacho(
                pedido.Id,
                guia,
                _proveedorLogistico,
                pedido.TipoEntrega,
                fabrica.CrearCalculadorPlazo().CalcularPlazoEnDias());

            await _despachoRepository.AgregarAsync(despacho, ct);

            var payload = JsonSerializer.Serialize(new
            {
                pedidoId = pedido.Id,
                numeroGuia = guia,
                proveedor = _proveedorLogistico,
                fecha = DateTime.UtcNow
            });
            await _outboxRepository.AgregarAsync(new EventoOutbox("DespachoRegistrado", payload), ct);

            await _auditoriaRepository.AgregarAsync(new Auditoria(
                usuario: "BODEGA",
                operacion: "DESPACHO_PEDIDO",
                entidad: "Pedido",
                entidadId: pedido.Id.ToString(),
                detalles: $"Guía: {guia} | Proveedor: {_proveedorLogistico}",
                exitoso: true), ct);

            await _unitOfWork.BeginTransactionAsync(ct);
            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitAsync(ct);

            await _outboxProcesador.ProcesarPendientesAsync(50, ct);

            return CommandResult.Ok($"Pedido {pedido.NumeroPedido} despachado con guía {guia}.");
        }
        catch (Domain.Exceptions.DomainException ex)
        {
            await _unitOfWork.RollbackAsync(ct);
            return CommandResult.Falla(ex.Message);
        }
        catch (ConflictoConcurrenciaException ex)
        {
            await _unitOfWork.RollbackAsync(ct);
            return CommandResult.Falla(ex.Message);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync(ct);
            return CommandResult.Falla($"Error al despachar pedido: {ex.Message}");
        }
    }
}
