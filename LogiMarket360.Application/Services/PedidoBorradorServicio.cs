namespace LogiMarket360.Application.Services;

using LogiMarket360.Application.DTOs;
using LogiMarket360.Application.Patterns.Builder;
using LogiMarket360.Application.Patterns.Prototype;
using LogiMarket360.Application.Ports;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Application.Security;
using LogiMarket360.Domain.Entities;
using LogiMarket360.Domain.Enums;
using LogiMarket360.Domain.Exceptions;
using LogiMarket360.Domain.Patterns.Composite;
using LogiMarket360.Domain.Patterns.Strategy;

public class PedidoBorradorServicio
{
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IProductoRepository _productoRepository;
    private readonly CatalogoServicio _catalogoServicio;
    private readonly IEntregaFactoryProvider _entregaProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAutorizador _autorizador;
    private readonly CalculadoraDescuento _calculadoraDescuento;

    public PedidoBorradorServicio(
        IPedidoRepository pedidoRepository,
        IClienteRepository clienteRepository,
        IProductoRepository productoRepository,
        CatalogoServicio catalogoServicio,
        IEntregaFactoryProvider entregaProvider,
        IUnitOfWork unitOfWork,
        IAutorizador autorizador,
        CalculadoraDescuento? calculadoraDescuento = null)
    {
        _pedidoRepository = pedidoRepository ?? throw new ArgumentNullException(nameof(pedidoRepository));
        _clienteRepository = clienteRepository ?? throw new ArgumentNullException(nameof(clienteRepository));
        _productoRepository = productoRepository ?? throw new ArgumentNullException(nameof(productoRepository));
        _catalogoServicio = catalogoServicio ?? throw new ArgumentNullException(nameof(catalogoServicio));
        _entregaProvider = entregaProvider ?? throw new ArgumentNullException(nameof(entregaProvider));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _autorizador = autorizador ?? throw new ArgumentNullException(nameof(autorizador));
        _calculadoraDescuento = calculadoraDescuento ?? new CalculadoraDescuento();
    }

    public IReadOnlyDictionary<TipoEntrega, (decimal Tarifa, int PlazoDias)> ObtenerTarifasEntrega()
    {
        return Enum.GetValues<TipoEntrega>().ToDictionary(
            tipo => tipo,
            tipo =>
            {
                var fabrica = _entregaProvider.Obtener(tipo);
                return (fabrica.CrearCalculadorTarifa().CalcularTarifa(), fabrica.CrearCalculadorPlazo().CalcularPlazoEnDias());
            });
    }

    public async Task<ResultadoOperacion> CrearBorradorAsync(
        int clienteId,
        TipoEntrega tipoEntrega,
        IReadOnlyList<LineaPedidoSolicitud> lineas,
        CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Ventas);

        var cliente = await _clienteRepository.ObtenerPorIdAsync(clienteId, ct);
        if (cliente is null)
            return new ResultadoOperacion(false, "Cliente no encontrado.");
        if (!cliente.Activo)
            return new ResultadoOperacion(false, "El cliente se encuentra inactivo.");

        try
        {
            var pedido = await ConstruirPedidoTemporalAsync(cliente, tipoEntrega, lineas, ct);
            await _pedidoRepository.AgregarAsync(pedido, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return new ResultadoOperacion(
                true,
                $"Borrador {pedido.NumeroPedido} creado por USD {pedido.Total:F2}.",
                pedido.Id);
        }
        catch (DomainException ex)
        {
            return new ResultadoOperacion(false, ex.Message);
        }
        catch (ArgumentException ex)
        {
            return new ResultadoOperacion(false, ex.Message);
        }
    }

    public async Task<ResultadoOperacion> ActualizarBorradorAsync(
        int pedidoId,
        TipoEntrega tipoEntrega,
        IReadOnlyList<LineaPedidoSolicitud> lineas,
        CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Ventas);

        var pedido = await _pedidoRepository.ObtenerPorIdAsync(pedidoId, ct);
        if (pedido is null)
            return new ResultadoOperacion(false, "Pedido no encontrado.");
        if (pedido.EstadoCodigo != EstadoPedido.BORRADOR)
            return new ResultadoOperacion(false, "Solo se pueden editar pedidos en estado BORRADOR.");

        var cliente = await _clienteRepository.ObtenerPorIdAsync(pedido.ClienteId, ct);
        if (cliente is null)
            return new ResultadoOperacion(false, "Cliente no encontrado.");

        try
        {
            var temporal = await ConstruirPedidoTemporalAsync(cliente, tipoEntrega, lineas, ct);

            pedido.LimpiarDetalles();
            foreach (var detalle in temporal.Detalles)
                pedido.AgregarDetalle(detalle);

            decimal tarifa = _entregaProvider.Obtener(tipoEntrega).CrearCalculadorTarifa().CalcularTarifa();
            pedido.AsignarTarifaEnvio(tarifa, tipoEntrega);
            pedido.RecalcularTotales(temporal.Descuento, temporal.EstrategiaDescuento);

            _pedidoRepository.Actualizar(pedido);
            await _unitOfWork.SaveChangesAsync(ct);

            return new ResultadoOperacion(true, $"Borrador {pedido.NumeroPedido} actualizado.", pedido.Id);
        }
        catch (DomainException ex)
        {
            return new ResultadoOperacion(false, ex.Message);
        }
        catch (ArgumentException ex)
        {
            return new ResultadoOperacion(false, ex.Message);
        }
    }

    public async Task<ResultadoOperacion> ClonarBorradorAsync(int pedidoId, CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Ventas);

        var original = await _pedidoRepository.ObtenerPorIdAsync(pedidoId, ct);
        if (original is null)
            return new ResultadoOperacion(false, "Pedido no encontrado.");
        if (original.EstadoCodigo != EstadoPedido.BORRADOR)
            return new ResultadoOperacion(false, "Solo se pueden clonar borradores.");

        var prototipo = new PedidoBorradorPrototype(original);
        var clon = prototipo.Clonar();

        await _pedidoRepository.AgregarAsync(clon, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new ResultadoOperacion(true, $"Borrador clonado como {clon.NumeroPedido}.", clon.Id);
    }

    public async Task<PedidoResumenDto?> ObtenerAsync(int pedidoId, CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Consulta);
        var pedido = await _pedidoRepository.ObtenerPorIdAsync(pedidoId, ct);
        return pedido is null ? null : ToDto(pedido);
    }

    public async Task<IReadOnlyList<PedidoResumenDto>> ListarAsync(CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Consulta);
        var pedidos = await _pedidoRepository.ListarTodosAsync(ct);
        return pedidos.Select(ToDto).ToList();
    }

    private async Task<Pedido> ConstruirPedidoTemporalAsync(
        Cliente cliente,
        TipoEntrega tipoEntrega,
        IReadOnlyList<LineaPedidoSolicitud> lineas,
        CancellationToken ct)
    {
        if (lineas is null || lineas.Count == 0)
            throw new DomainException("El pedido debe tener al menos una línea.");

        var fabrica = _entregaProvider.Obtener(tipoEntrega);
        decimal tarifa = fabrica.CrearCalculadorTarifa().CalcularTarifa();

        var builder = new PedidoBuilder(_calculadoraDescuento)
            .ParaCliente(cliente.Id, cliente.Categoria)
            .ConEntrega(tipoEntrega, tarifa);

        foreach (var linea in lineas)
        {
            if (linea.Cantidad <= 0)
                throw new DomainException($"La cantidad de '{linea.Codigo}' debe ser mayor a cero.");

            if (linea.EsKit)
            {
                var kit = await _catalogoServicio.ObtenerArbolKitAsync(linea.Codigo, ct);
                builder.AgregarKit(kit, linea.Cantidad);
            }
            else
            {
                var producto = await _productoRepository.ObtenerPorSkuAsync(linea.Codigo, ct)
                    ?? throw new DomainException($"El producto '{linea.Codigo}' no existe.");

                builder.AgregarProducto(
                    new ProductoSimple(producto.Sku, producto.Nombre, producto.Precio, producto.PesoKg),
                    linea.Cantidad);
            }
        }

        return builder.Build();
    }

    public static PedidoResumenDto ToDto(Pedido pedido)
    {
        var detalles = pedido.Detalles.Select(d => new PedidoDetalleDto(
            d.CodigoItem,
            d.NombreItem,
            d.EsKit,
            d.Cantidad,
            d.PrecioUnitarioSnapshot,
            d.SubtotalLinea,
            d.ComponentesSnapshot.Select(c => new ComponentePedidoDto(
                c.Sku,
                c.Nombre,
                c.CantidadFisica,
                c.PrecioUnitarioSnapshot,
                c.PesoKgSnapshot)).ToList()
        )).ToList();

        return new PedidoResumenDto(
            pedido.Id,
            pedido.NumeroPedido,
            pedido.ClienteId,
            pedido.Cliente?.Nombre ?? $"Cliente {pedido.ClienteId}",
            pedido.Cliente?.Categoria ?? CategoriaCliente.NORMAL,
            pedido.EstadoCodigo,
            pedido.TipoEntrega,
            pedido.Subtotal,
            pedido.Descuento,
            pedido.EstrategiaDescuento,
            pedido.CostoEnvio,
            pedido.Total,
            pedido.FechaCreacion,
            pedido.IdempotencyKey,
            detalles);
    }
}
