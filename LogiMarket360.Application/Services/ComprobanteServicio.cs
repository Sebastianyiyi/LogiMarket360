namespace LogiMarket360.Application.Services;

using LogiMarket360.Application.Documents;
using LogiMarket360.Application.DTOs;
using LogiMarket360.Application.Ports;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Application.Security;

public class ComprobanteServicio
{
    private readonly ISalidaDocumentoFactory _salidaFactory;
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IPagoRepository _pagoRepository;
    private readonly IDevolucionRepository _devolucionRepository;
    private readonly IAutorizador _autorizador;

    public ComprobanteServicio(
        ISalidaDocumentoFactory salidaFactory,
        IPedidoRepository pedidoRepository,
        IPagoRepository pagoRepository,
        IDevolucionRepository devolucionRepository,
        IAutorizador autorizador)
    {
        _salidaFactory = salidaFactory;
        _pedidoRepository = pedidoRepository;
        _pagoRepository = pagoRepository;
        _devolucionRepository = devolucionRepository;
        _autorizador = autorizador;
    }

    public IReadOnlyList<string> MediosDisponibles => _salidaFactory.MediosDisponibles;

    public async Task<ResultadoComprobante> EmitirVentaAsync(
        int pedidoId,
        string medio,
        CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Consulta);

        var pedido = await _pedidoRepository.ObtenerPorIdAsync(pedidoId, ct);
        if (pedido is null)
            return new ResultadoComprobante(false, "Pedido no encontrado.");

        var pago = await _pagoRepository.ObtenerPorPedidoIdAsync(pedidoId, ct);
        var datos = new DatosComprobanteVenta(
            pedido.NumeroPedido,
            pedido.Cliente?.Nombre ?? $"Cliente {pedido.ClienteId}",
            pedido.EstrategiaDescuento,
            pedido.Subtotal,
            pedido.Descuento,
            pedido.CostoEnvio,
            pedido.Total,
            pago?.ReferenciaTransaccion ?? "SIN_PAGO",
            DateTime.UtcNow);

        var comprobante = new ComprobanteVenta(_salidaFactory.Crear(medio), datos);
        string resultado = await comprobante.EmitirAsync(datos, ct);
        return new ResultadoComprobante(true, $"Comprobante emitido por {medio}.", resultado);
    }

    public async Task<ResultadoComprobante> EmitirDevolucionAsync(
        int pedidoId,
        string medio,
        CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Consulta);

        var pedido = await _pedidoRepository.ObtenerPorIdAsync(pedidoId, ct);
        if (pedido is null)
            return new ResultadoComprobante(false, "Pedido no encontrado.");

        var devolucion = await _devolucionRepository.ObtenerPorPedidoIdAsync(pedidoId, ct);
        if (devolucion is null || !devolucion.Aprobada)
            return new ResultadoComprobante(false, "El pedido no tiene una devolución aprobada.");

        var datos = new DatosComprobanteDevolucion(
            pedido.NumeroPedido,
            pedido.Cliente?.Nombre ?? $"Cliente {pedido.ClienteId}",
            devolucion.MontoReembolso,
            devolucion.Motivo.ToString(),
            devolucion.ReferenciaReembolso,
            devolucion.FechaSolicitud);

        var comprobante = new ComprobanteDevolucion(_salidaFactory.Crear(medio), datos);
        string resultado = await comprobante.EmitirAsync(datos, ct);
        return new ResultadoComprobante(true, $"Comprobante de devolución emitido por {medio}.", resultado);
    }
}
