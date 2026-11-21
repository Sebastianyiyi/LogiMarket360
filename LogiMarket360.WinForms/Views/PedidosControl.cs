namespace LogiMarket360.WinForms.Views;

using System.ComponentModel;
using LogiMarket360.Application.DTOs;
using LogiMarket360.Application.Patterns.Command;
using LogiMarket360.Application.Patterns.Mediator;
using LogiMarket360.Application.Services;
using LogiMarket360.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

public sealed class PedidosControl : UserControl, IComponenteVista
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IFormularioPrincipalMediator _mediator;
    private readonly ComboBox _clientes = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly ComboBox _elementos = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
    private readonly ComboBox _entrega = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    private readonly NumericUpDown _cantidad = new() { Minimum = 1, Maximum = 999, Value = 1, Width = 65 };
    private readonly BindingList<LineaVisual> _lineas = new();
    private readonly DataGridView _carrito = CrearGrid();
    private readonly DataGridView _pedidos = CrearGrid();
    private readonly Label _estado = new() { AutoSize = true, ForeColor = Color.FromArgb(24, 62, 93) };

    public PedidosControl(
        IServiceScopeFactory scopeFactory,
        IFormularioPrincipalMediator mediator)
    {
        _scopeFactory = scopeFactory;
        _mediator = mediator;
        Dock = DockStyle.Fill;
        _entrega.DataSource = Enum.GetValues<TipoEntrega>();
        _carrito.DataSource = _lineas;
        Construir();
        _mediator.Registrar(this);
    }

    public AreaVista Area => AreaVista.Carrito;

    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); _ = CargarAsync(); }
    protected override void Dispose(bool disposing) { if (disposing) _mediator.Desregistrar(this); base.Dispose(disposing); }
    public Task ActualizarAsync(CambioAplicacion cambio, CancellationToken ct = default) => CargarCatalogoAsync(ct);

    private void Construir()
    {
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 285 };
        var creador = new Panel { Dock = DockStyle.Fill };
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(5) };
        var agregar = new Button { Text = "Agregar línea" };
        agregar.Click += (_, _) => AgregarLinea();
        var quitar = new Button { Text = "Quitar línea" };
        quitar.Click += (_, _) => QuitarLinea();
        var crear = new Button { Text = "Guardar borrador" };
        crear.Click += async (_, _) => await CrearBorradorAsync();
        top.Controls.AddRange(new Control[] { _clientes, _elementos, _cantidad, _entrega, agregar, quitar, crear });
        creador.Controls.Add(_carrito);
        creador.Controls.Add(top);
        split.Panel1.Controls.Add(creador);

        var listado = new Panel { Dock = DockStyle.Fill };
        var acciones = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(5) };
        var refrescar = new Button { Text = "Refrescar" };
        refrescar.Click += async (_, _) => await CargarPedidosAsync();
        var confirmar = new Button { Text = "Confirmar y cobrar" };
        confirmar.Click += async (_, _) => await ConfirmarAsync();
        var cancelar = new Button { Text = "Cancelar" };
        cancelar.Click += async (_, _) => await CancelarAsync();
        var clonar = new Button { Text = "Clonar borrador" };
        clonar.Click += async (_, _) => await ClonarAsync();
        acciones.Controls.AddRange(new Control[] { refrescar, confirmar, cancelar, clonar, _estado });
        listado.Controls.Add(_pedidos);
        listado.Controls.Add(acciones);
        split.Panel2.Controls.Add(listado);
        Controls.Add(split);
    }

    private async Task CargarAsync()
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var clientes = await scope.ServiceProvider.GetRequiredService<ClienteServicio>().ListarAsync();
            _clientes.DataSource = clientes.Where(c => c.Activo).Select(c => new Opcion(c.Id, c.Nombre, c)).ToList();
            _clientes.DisplayMember = nameof(Opcion.Texto);
            await CargarCatalogoAsync();
            await CargarPedidosAsync();
        }
        catch (Exception ex) { _estado.Text = ex.Message; }
    }

    private async Task CargarCatalogoAsync(CancellationToken ct = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var servicio = scope.ServiceProvider.GetRequiredService<CatalogoServicio>();
            var productos = await servicio.ListarProductosAsync(ct);
            var kits = await servicio.ListarKitsAsync(ct);
            var opciones = productos.Select(p => new ElementoOpcion(p.Sku, $"{p.Sku} · {p.Nombre} · USD {p.Precio:F2}", false))
                .Concat(kits.Select(k => new ElementoOpcion(k.Codigo, $"{k.Codigo} · {k.Nombre} · USD {k.Precio:F2}", true)))
                .ToList();
            _elementos.DataSource = opciones;
            _elementos.DisplayMember = nameof(ElementoOpcion.Texto);
        }
        catch (Exception ex) { _estado.Text = ex.Message; }
    }

    private async Task CargarPedidosAsync()
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var pedidos = await scope.ServiceProvider.GetRequiredService<PedidoBorradorServicio>().ListarAsync();
            _pedidos.DataSource = pedidos.Select(p => new PedidoFila(
                p.Id, p.NumeroPedido, p.Cliente, p.Estado, p.TipoEntrega,
                p.Subtotal, p.Descuento, p.CostoEnvio, p.Total, p.IdempotencyKey)).ToList();
            _estado.Text = $"{pedidos.Count} pedidos";
        }
        catch (Exception ex) { _estado.Text = ex.Message; }
    }

    private void AgregarLinea()
    {
        if (_elementos.SelectedItem is not ElementoOpcion item) return;
        var existente = _lineas.FirstOrDefault(x => x.Codigo == item.Codigo && x.EsKit == item.EsKit);
        if (existente is null) _lineas.Add(new LineaVisual(item.Codigo, item.EsKit, (int)_cantidad.Value));
        else existente.Cantidad += (int)_cantidad.Value;
        _carrito.Refresh();
    }

    private void QuitarLinea()
    {
        if (_carrito.CurrentRow?.DataBoundItem is LineaVisual linea) _lineas.Remove(linea);
    }

    private async Task CrearBorradorAsync()
    {
        if (_clientes.SelectedItem is not Opcion cliente) { _estado.Text = "Seleccione un cliente."; return; }
        var solicitudes = _lineas.Select(x => new LineaPedidoSolicitud(x.Codigo, x.Cantidad, x.EsKit)).ToList();
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var resultado = await scope.ServiceProvider.GetRequiredService<PedidoBorradorServicio>()
                .CrearBorradorAsync(cliente.Id, (TipoEntrega)_entrega.SelectedItem!, solicitudes);
            _estado.Text = resultado.Mensaje;
            if (resultado.Exitoso) { _lineas.Clear(); await CargarPedidosAsync(); }
        }
        catch (Exception ex) { _estado.Text = ex.Message; }
    }

    private async Task ConfirmarAsync()
    {
        if (_pedidos.CurrentRow?.DataBoundItem is not PedidoFila pedido) return;
        await using var scope = _scopeFactory.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<ICommandFactory>();
        var resultado = await scope.ServiceProvider.GetRequiredService<ICommandInvoker>()
            .InvocarAsync(factory.CrearConfirmar(pedido.Id, pedido.ClaveIdempotencia));
        _estado.Text = resultado.Mensaje;
        await CargarPedidosAsync();
        if (resultado.Exitoso)
            await _mediator.NotificarAsync(this, new CambioAplicacion("PedidoConfirmado", new[] { AreaVista.Catalogo, AreaVista.Pedidos, AreaVista.Metricas }));
    }

    private async Task CancelarAsync()
    {
        if (_pedidos.CurrentRow?.DataBoundItem is not PedidoFila pedido) return;
        await using var scope = _scopeFactory.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<ICommandFactory>();
        var resultado = await scope.ServiceProvider.GetRequiredService<ICommandInvoker>()
            .InvocarAsync(factory.CrearCancelar(pedido.Id));
        _estado.Text = resultado.Mensaje;
        await CargarPedidosAsync();
    }

    private async Task ClonarAsync()
    {
        if (_pedidos.CurrentRow?.DataBoundItem is not PedidoFila pedido) return;
        await using var scope = _scopeFactory.CreateAsyncScope();
        var resultado = await scope.ServiceProvider.GetRequiredService<PedidoBorradorServicio>()
            .ClonarBorradorAsync(pedido.Id);
        _estado.Text = resultado.Mensaje;
        if (resultado.Exitoso) await CargarPedidosAsync();
    }

    private static DataGridView CrearGrid() => new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false
    };

    private sealed record Opcion(int Id, string Texto, ClienteDto Cliente);
    private sealed record ElementoOpcion(string Codigo, string Texto, bool EsKit);
    private sealed class LineaVisual
    {
        public LineaVisual(string codigo, bool esKit, int cantidad) { Codigo = codigo; EsKit = esKit; Cantidad = cantidad; }
        public string Codigo { get; }
        public bool EsKit { get; }
        public int Cantidad { get; set; }
    }
    private sealed record PedidoFila(int Id, string Numero, string Cliente, EstadoPedido Estado, TipoEntrega Entrega,
        decimal Subtotal, decimal Descuento, decimal Envio, decimal Total, string ClaveIdempotencia);
}
