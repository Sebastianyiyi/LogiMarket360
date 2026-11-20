namespace LogiMarket360.WinForms.Views;

using LogiMarket360.Application.Patterns.Command;
using LogiMarket360.Application.Patterns.Mediator;
using LogiMarket360.Application.Patterns.TemplateMethod;
using LogiMarket360.Application.Services;
using LogiMarket360.Domain.Enums;

public sealed class DevolucionesControl : UserControl, IComponenteVista
{
    private readonly ReporteServicio _reportes;
    private readonly ICommandFactory _factory;
    private readonly ICommandInvoker _invoker;
    private readonly IFormularioPrincipalMediator _mediator;
    private readonly DataGridView _grid = Grid();
    private readonly ComboBox _motivo = new() { DataSource = Enum.GetValues<MotivoDevolucion>(), DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    private readonly NumericUpDown _dias = new() { Minimum = 0, Maximum = 3650, Width = 70 };
    private readonly CheckBox _sinUso = new() { Text = "Producto sin uso", AutoSize = true };
    private readonly TextBox _detalle = new() { PlaceholderText = "Detalle del motivo", Width = 240 };
    private readonly Label _estado = new() { AutoSize = true };

    public DevolucionesControl(ReporteServicio reportes, ICommandFactory factory, ICommandInvoker invoker, IFormularioPrincipalMediator mediator)
    {
        _reportes = reportes; _factory = factory; _invoker = invoker; _mediator = mediator;
        Dock = DockStyle.Fill;
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 76, Padding = new Padding(5), AutoScroll = true };
        var process = new Button { Text = "Procesar devolución" }; process.Click += async (_, _) => await ProcesarAsync();
        var refresh = new Button { Text = "Refrescar" }; refresh.Click += async (_, _) => await CargarAsync();
        bar.Controls.AddRange(new Control[] { _motivo, new Label { Text = "Días desde entrega:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, _dias, _sinUso, _detalle, process, refresh, _estado });
        Controls.Add(_grid); Controls.Add(bar); _mediator.Registrar(this);
    }

    public AreaVista Area => AreaVista.Pedidos;
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); _ = CargarAsync(); }
    protected override void Dispose(bool disposing) { if (disposing) _mediator.Desregistrar(this); base.Dispose(disposing); }
    public Task ActualizarAsync(CambioAplicacion cambio, CancellationToken ct = default) => CargarAsync(ct);

    private async Task CargarAsync(CancellationToken ct = default)
    {
        try
        {
            var pedidos = await _reportes.ListarPedidosAsync(ct);
            var devoluciones = await _reportes.ListarDevolucionesAsync(ct);
            _grid.DataSource = pedidos.Where(p => p.Estado == EstadoPedido.ENTREGADO).Select(p => new Fila(
                p.Id, p.NumeroPedido, p.Cliente, p.Total,
                devoluciones.FirstOrDefault(d => d.PedidoId == p.Id)?.Aprobada,
                devoluciones.FirstOrDefault(d => d.PedidoId == p.Id)?.MontoReembolso)).ToList();
            _estado.Text = $"{devoluciones.Count} devoluciones registradas";
        }
        catch (Exception ex) { _estado.Text = ex.Message; }
    }

    private async Task ProcesarAsync()
    {
        if (_grid.CurrentRow?.DataBoundItem is not Fila fila) return;
        var solicitud = new SolicitudDevolucion(fila.Id, (MotivoDevolucion)_motivo.SelectedItem!, (int)_dias.Value, _sinUso.Checked, _detalle.Text);
        var result = await _invoker.InvocarAsync(_factory.CrearDevolucion(solicitud));
        _estado.Text = result.Mensaje; await CargarAsync();
        if (result.Exitoso) await _mediator.NotificarAsync(this, new CambioAplicacion("DevolucionAceptada", new[] { AreaVista.Catalogo, AreaVista.Metricas }));
    }

    private static DataGridView Grid() => new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false };
    private sealed record Fila(int Id, string Pedido, string Cliente, decimal Total, bool? Devuelto, decimal? Reembolso);
}
