namespace LogiMarket360.Application.Patterns.Command;

using LogiMarket360.Application.Facade;
using LogiMarket360.Application.Patterns.TemplateMethod;
using LogiMarket360.Application.Ports;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Application.Security;

public interface ICommandFactory
{
    ICommand CrearConfirmar(int pedidoId, string claveIdempotencia);
    ICommand CrearCancelar(int pedidoId);
    ICommand CrearDespachar(int pedidoId, string? numeroGuia, string proveedorLogistico);
    ICommand CrearEntregar(int pedidoId);
    ICommand CrearDevolucion(SolicitudDevolucion solicitud);
}

public sealed class CommandFactory : ICommandFactory
{
    private readonly ConfirmacionPedidoFacade _confirmacion;
    private readonly IPedidoRepository _pedidos;
    private readonly IDespachoRepository _despachos;
    private readonly IAuditoriaRepository _auditorias;
    private readonly IEventoOutboxRepository _outbox;
    private readonly IOutboxProcesador _outboxProcesador;
    private readonly IEntregaFactoryProvider _entregas;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAutorizador _autorizador;
    private readonly DevolucionDefectoService _devolucionDefecto;
    private readonly DevolucionArrepentimientoService _devolucionArrepentimiento;

    public CommandFactory(
        ConfirmacionPedidoFacade confirmacion,
        IPedidoRepository pedidos,
        IDespachoRepository despachos,
        IAuditoriaRepository auditorias,
        IEventoOutboxRepository outbox,
        IOutboxProcesador outboxProcesador,
        IEntregaFactoryProvider entregas,
        IUnitOfWork unitOfWork,
        IAutorizador autorizador,
        DevolucionDefectoService devolucionDefecto,
        DevolucionArrepentimientoService devolucionArrepentimiento)
    {
        _confirmacion = confirmacion;
        _pedidos = pedidos;
        _despachos = despachos;
        _auditorias = auditorias;
        _outbox = outbox;
        _outboxProcesador = outboxProcesador;
        _entregas = entregas;
        _unitOfWork = unitOfWork;
        _autorizador = autorizador;
        _devolucionDefecto = devolucionDefecto;
        _devolucionArrepentimiento = devolucionArrepentimiento;
    }

    public ICommand CrearConfirmar(int pedidoId, string claveIdempotencia) =>
        new ConfirmarPedidoCommand(pedidoId, claveIdempotencia, _confirmacion);

    public ICommand CrearCancelar(int pedidoId) =>
        new CancelarPedidoCommand(pedidoId, _pedidos, _unitOfWork, _auditorias, _autorizador);

    public ICommand CrearDespachar(int pedidoId, string? numeroGuia, string proveedorLogistico) =>
        new DespacharPedidoCommand(
            pedidoId,
            numeroGuia,
            proveedorLogistico,
            _pedidos,
            _despachos,
            _entregas,
            _outbox,
            _outboxProcesador,
            _auditorias,
            _unitOfWork,
            _autorizador);

    public ICommand CrearEntregar(int pedidoId) =>
        new EntregarPedidoCommand(
            pedidoId,
            _pedidos,
            _despachos,
            _auditorias,
            _unitOfWork,
            _autorizador);

    public ICommand CrearDevolucion(SolicitudDevolucion solicitud) =>
        new ProcesarDevolucionCommand(solicitud, _devolucionDefecto, _devolucionArrepentimiento);
}
