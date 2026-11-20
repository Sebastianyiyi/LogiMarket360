namespace LogiMarket360.Domain.Entities;

using LogiMarket360.Domain.Enums;
using LogiMarket360.Domain.Exceptions;
using LogiMarket360.Domain.Patterns.State;

public class Pedido
{
    public int Id { get; set; }
    public string NumeroPedido { get; private set; } = string.Empty;
    public int ClienteId { get; private set; }
    public virtual Cliente? Cliente { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public EstadoPedido EstadoCodigo { get; private set; }
    public TipoEntrega TipoEntrega { get; private set; }
    public decimal CostoEnvio { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal Descuento { get; private set; }
    public decimal Total { get; private set; }
    public string EstrategiaDescuento { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;

    private IPedidoEstado _estado;
    public IPedidoEstado Estado => _estado;

    private readonly List<PedidoDetalle> _detalles = new();
    public virtual IReadOnlyCollection<PedidoDetalle> Detalles => _detalles.AsReadOnly();

    protected Pedido()
    {
        _estado = new BorradorEstado();
        EstadoCodigo = EstadoPedido.BORRADOR;
    }

    public Pedido(
        string numeroPedido,
        int clienteId,
        TipoEntrega tipoEntrega,
        decimal costoEnvio,
        string idempotencyKey = "")
    {
        if (string.IsNullOrWhiteSpace(numeroPedido))
            throw new ArgumentException("El número de pedido no puede ser vacío.", nameof(numeroPedido));
        if (clienteId <= 0)
            throw new ArgumentOutOfRangeException(nameof(clienteId), "El ID de cliente debe ser válido.");
        if (costoEnvio < 0)
            throw new ArgumentOutOfRangeException(nameof(costoEnvio), "La tarifa de entrega no puede ser negativa.");

        NumeroPedido = numeroPedido.Trim().ToUpperInvariant();
        ClienteId = clienteId;
        TipoEntrega = tipoEntrega;
        CostoEnvio = decimal.Round(costoEnvio, 2, MidpointRounding.AwayFromZero);
        FechaCreacion = DateTime.UtcNow;
        IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? Guid.NewGuid().ToString() : idempotencyKey;

        _estado = new BorradorEstado();
        EstadoCodigo = EstadoPedido.BORRADOR;
    }

    public void SincronizarEstadoDesdePersistencia()
    {
        _estado = PedidoEstadoFactory.CrearEstado(EstadoCodigo);
    }

    public void CambiarEstado(IPedidoEstado nuevoEstado)
    {
        ArgumentNullException.ThrowIfNull(nuevoEstado);
        _estado = nuevoEstado;
        EstadoCodigo = nuevoEstado.CodigoEstado;
    }

    public void Validar() => _estado.Validar(this);
    public void Pagar() => _estado.Pagar(this);
    public void Despachar() => _estado.Despachar(this);
    public void Entregar() => _estado.Entregar(this);
    public void Cancelar() => _estado.Cancelar(this);

    public void AgregarDetalle(PedidoDetalle detalle)
    {
        if (EstadoCodigo != EstadoPedido.BORRADOR)
            throw new DomainException($"No se pueden modificar líneas en un pedido con estado '{EstadoCodigo}'.");

        ArgumentNullException.ThrowIfNull(detalle);
        _detalles.Add(detalle);
    }

    public void LimpiarDetalles()
    {
        if (EstadoCodigo != EstadoPedido.BORRADOR)
            throw new DomainException($"Solo se pueden editar líneas de un pedido en BORRADOR. Estado actual: '{EstadoCodigo}'.");

        _detalles.Clear();
    }

    public void RecalcularTotales(decimal descuentoCalculado, string nombreEstrategia)
    {
        if (EstadoCodigo != EstadoPedido.BORRADOR && EstadoCodigo != EstadoPedido.VALIDADO)
            throw new DomainException($"No se pueden recalcular valores en un pedido con estado '{EstadoCodigo}'.");

        Subtotal = decimal.Round(_detalles.Sum(d => d.SubtotalLinea), 2, MidpointRounding.AwayFromZero);
        Descuento = decimal.Round(descuentoCalculado, 2, MidpointRounding.AwayFromZero);
        EstrategiaDescuento = nombreEstrategia;
        Total = decimal.Round(Subtotal - Descuento + CostoEnvio, 2, MidpointRounding.AwayFromZero);
    }

    public void AsignarTarifaEnvio(decimal nuevaTarifa, TipoEntrega nuevoTipo)
    {
        if (EstadoCodigo != EstadoPedido.BORRADOR && EstadoCodigo != EstadoPedido.VALIDADO)
            throw new DomainException($"No se puede alterar el costo de envío en un pedido con estado '{EstadoCodigo}'.");

        CostoEnvio = decimal.Round(nuevaTarifa, 2, MidpointRounding.AwayFromZero);
        TipoEntrega = nuevoTipo;
        Total = decimal.Round(Subtotal - Descuento + CostoEnvio, 2, MidpointRounding.AwayFromZero);
    }

    public int ContarTotalUnidadesFisicas()
    {
        int total = 0;
        foreach (var detalle in _detalles)
        {
            if (detalle.ComponentesSnapshot.Count > 0)
            {
                total += detalle.ComponentesSnapshot.Sum(c => c.CantidadFisica);
            }
            else
            {
                total += detalle.Cantidad;
            }
        }
        return total;
    }
}
