namespace LogiMarket360.Application.Patterns.Builder;

using LogiMarket360.Domain.Entities;
using LogiMarket360.Domain.Enums;
using LogiMarket360.Domain.Exceptions;
using LogiMarket360.Domain.Patterns.Composite;
using LogiMarket360.Domain.Patterns.Strategy;

public class PedidoBuilder
{
    private string _numeroPedido = string.Empty;
    private int _clienteId = 0;
    private CategoriaCliente _categoriaCliente = CategoriaCliente.NORMAL;
    private TipoEntrega _tipoEntrega = TipoEntrega.ESTANDAR;
    private decimal _costoEnvio = 5.00m;
    private string _idempotencyKey = string.Empty;
    private readonly List<PedidoDetalle> _detalles = new();
    private readonly CalculadoraDescuento _calculadoraDescuento;

    public PedidoBuilder(CalculadoraDescuento? calculadoraDescuento = null)
    {
        _calculadoraDescuento = calculadoraDescuento ?? new CalculadoraDescuento();
    }

    public PedidoBuilder ConNumero(string numeroPedido)
    {
        _numeroPedido = numeroPedido;
        return this;
    }

    public PedidoBuilder ParaCliente(int clienteId, CategoriaCliente categoriaCliente)
    {
        _clienteId = clienteId;
        _categoriaCliente = categoriaCliente;
        return this;
    }

    public PedidoBuilder ConEntrega(TipoEntrega tipoEntrega, decimal costoEnvio)
    {
        _tipoEntrega = tipoEntrega;
        _costoEnvio = costoEnvio;
        return this;
    }

    public PedidoBuilder ConIdempotencia(string idempotencyKey)
    {
        _idempotencyKey = idempotencyKey;
        return this;
    }

    public PedidoBuilder AgregarProducto(ProductoSimple producto, int cantidad)
    {
        ArgumentNullException.ThrowIfNull(producto);
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor a cero.");

        var componentes = new List<PedidoDetalleComponente>
        {
            new(producto.Sku, producto.Nombre, cantidad, producto.Precio, producto.PesoKg)
        };

        var detalle = new PedidoDetalle(
            codigoItem: producto.Sku,
            nombreItem: producto.Nombre,
            esKit: false,
            cantidad: cantidad,
            precioUnitarioSnapshot: producto.Precio,
            componentes: componentes
        );

        _detalles.Add(detalle);
        return this;
    }

    public PedidoBuilder AgregarKit(KitCompuesto kit, int cantidad)
    {
        ArgumentNullException.ThrowIfNull(kit);
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor a cero.");

        var desglosados = kit.DesglosarComponentes(cantidad);
        var componentes = desglosados.Select(c => new PedidoDetalleComponente(
            sku: c.Sku,
            nombre: c.Nombre,
            cantidadFisica: c.Cantidad,
            precioUnitarioSnapshot: c.PrecioUnitario,
            pesoKgSnapshot: c.PesoKg
        )).ToList();

        var detalle = new PedidoDetalle(
            codigoItem: kit.CodigoKit,
            nombreItem: kit.Nombre,
            esKit: true,
            cantidad: cantidad,
            precioUnitarioSnapshot: kit.ObtenerPrecio(),
            componentes: componentes
        );

        _detalles.Add(detalle);
        return this;
    }

    public Pedido Build()
    {
        if (string.IsNullOrWhiteSpace(_numeroPedido))
            _numeroPedido = $"PED-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

        if (_clienteId <= 0)
            throw new DomainException("No se puede construir un pedido sin un cliente válido.");

        if (_detalles.Count == 0)
            throw new DomainException("No se puede construir un pedido sin líneas de productos o kits.");

        var pedido = new Pedido(
            _numeroPedido,
            _clienteId,
            _tipoEntrega,
            _costoEnvio,
            _idempotencyKey
        );

        foreach (var detalle in _detalles)
        {
            pedido.AgregarDetalle(detalle);
        }

        // Calcular unidades físicas totales y aplicar la estrategia de descuento
        int totalUnidadesFisicas = pedido.ContarTotalUnidadesFisicas();
        decimal subtotal = _detalles.Sum(d => d.SubtotalLinea);

        var contexto = new ContextoDescuento(_categoriaCliente, totalUnidadesFisicas, subtotal);
        var (nombreEstrategia, descuento) = _calculadoraDescuento.Calcular(contexto);

        pedido.RecalcularTotales(descuento, nombreEstrategia);
        return pedido;
    }
}
