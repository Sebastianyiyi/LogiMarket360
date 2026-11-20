namespace LogiMarket360.Application.Services;

using LogiMarket360.Application.DTOs;
using LogiMarket360.Application.Patterns.Interpreter;
using LogiMarket360.Application.Patterns.Singleton;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Application.Security;
using LogiMarket360.Domain.Entities;

public class ReporteServicio
{
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IMovimientoInventarioRepository _movimientoRepository;
    private readonly IAuditoriaRepository _auditoriaRepository;
    private readonly IPagoRepository _pagoRepository;
    private readonly IDespachoRepository _despachoRepository;
    private readonly IDevolucionRepository _devolucionRepository;
    private readonly MetricasSesion _metricas;
    private readonly IAutorizador _autorizador;
    private readonly PedidoQueryParser _parser = new();

    public ReporteServicio(
        IPedidoRepository pedidoRepository,
        IMovimientoInventarioRepository movimientoRepository,
        IAuditoriaRepository auditoriaRepository,
        IPagoRepository pagoRepository,
        IDespachoRepository despachoRepository,
        IDevolucionRepository devolucionRepository,
        MetricasSesion metricas,
        IAutorizador autorizador)
    {
        _pedidoRepository = pedidoRepository;
        _movimientoRepository = movimientoRepository;
        _auditoriaRepository = auditoriaRepository;
        _pagoRepository = pagoRepository;
        _despachoRepository = despachoRepository;
        _devolucionRepository = devolucionRepository;
        _metricas = metricas;
        _autorizador = autorizador;
    }

    public async Task<IReadOnlyList<PedidoResumenDto>> FiltrarPedidosAsync(
        string expresion,
        CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Consulta);

        var pedidos = await _pedidoRepository.ListarTodosAsync(ct);
        IExpression filtro = _parser.Parse(expresion);

        return pedidos
            .Where(p => filtro.Interpretar(new ContextoPedido(
                p,
                p.Cliente?.Nombre ?? $"Cliente {p.ClienteId}")))
            .Select(PedidoBorradorServicio.ToDto)
            .ToList();
    }

    public async Task<IReadOnlyList<PedidoResumenDto>> ListarPedidosAsync(CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Consulta);
        var pedidos = await _pedidoRepository.ListarTodosAsync(ct);
        return pedidos.Select(PedidoBorradorServicio.ToDto).ToList();
    }

    public async Task<IReadOnlyList<MovimientoInventarioDto>> ListarMovimientosAsync(CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Consulta);
        var movimientos = await _movimientoRepository.ListarTodosAsync(ct);
        return movimientos.Select(m => new MovimientoInventarioDto(
            m.Id, m.ProductoId, m.Sku, m.Cantidad, m.TipoMovimiento, m.Motivo, m.Referencia, m.Fecha)).ToList();
    }

    public async Task<IReadOnlyList<Auditoria>> ListarAuditoriaAsync(int limite = 200, CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Consulta);
        return await _auditoriaRepository.ListarRecientesAsync(limite, ct);
    }

    public async Task<IReadOnlyList<PagoDto>> ListarPagosAsync(CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Consulta);
        var pagos = await _pagoRepository.ListarTodosAsync(ct);
        return pagos.Select(p => new PagoDto(
            p.Id, p.PedidoId, p.Monto, p.ReferenciaTransaccion, p.ClaveIdempotencia,
            p.ProveedorSimulado, p.FechaPago, p.Aprobado)).ToList();
    }

    public async Task<IReadOnlyList<DespachoDto>> ListarDespachosAsync(CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Consulta);
        var despachos = await _despachoRepository.ListarTodosAsync(ct);
        return despachos.Select(d => new DespachoDto(
            d.Id, d.PedidoId, d.NumeroGuia, d.ProveedorLogistico, d.TipoEntrega,
            d.PlazoEstimadoDias, d.FechaDespacho, d.FechaEntregaEstimada, d.FechaEntregaReal)).ToList();
    }

    public async Task<IReadOnlyList<DevolucionDto>> ListarDevolucionesAsync(CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Consulta);
        var devoluciones = await _devolucionRepository.ListarTodasAsync(ct);
        return devoluciones.Select(d => new DevolucionDto(
            d.Id, d.PedidoId, d.Motivo, d.FechaSolicitud, d.DiasDesdeEntrega, d.ProductoSinUso,
            d.Aprobada, d.MontoReembolso, d.ReintegroStockRealizado, d.ReferenciaReembolso,
            d.DetalleMotivo)).ToList();
    }

    public MetricasSesionDto ObtenerMetricas()
    {
        _autorizador.ExigirRol(Permisos.Consulta);
        return new MetricasSesionDto(
            _metricas.TotalPedidosConfirmados,
            _metricas.MontoTotalVentas,
            _metricas.TotalDevolucionesAceptadas,
            _metricas.MontoTotalReembolsos,
            _metricas.IntentosCobroFallidos,
            _metricas.InicioSesion);
    }
}
