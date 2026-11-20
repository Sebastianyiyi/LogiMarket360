namespace LogiMarket360.WinForms.Views;

using LogiMarket360.Application.Patterns.Mediator;
using LogiMarket360.Application.Services;

public sealed class ConsultasControl : UserControl, IComponenteVista
{
    private readonly ReporteServicio _reportes;
    private readonly ComprobanteServicio _comprobantes;
    private readonly IFormularioPrincipalMediator _mediator;
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly TextBox _filtro = new() { PlaceholderText = "ESTADO = PAGADO AND TOTAL >= 50", Width = 330 };
    private readonly Label _estado = new() { AutoSize = true };
    private readonly DataGridView _pedidos = Grid();

    public ConsultasControl(ReporteServicio reportes, ComprobanteServicio comprobantes, IFormularioPrincipalMediator mediator)
    {
        _reportes = reportes; _comprobantes = comprobantes; _mediator = mediator;
        Dock = DockStyle.Fill; Construir(); _mediator.Registrar(this);
    }

    public AreaVista Area => AreaVista.Metricas;
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); _ = CargarAsync(); }
    protected override void Dispose(bool disposing) { if (disposing) _mediator.Desregistrar(this); base.Dispose(disposing); }
    public Task ActualizarAsync(CambioAplicacion cambio, CancellationToken ct = default) => CargarAsync(ct);

    private void Construir()
    {
        var pagePedidos = new TabPage("Pedidos");
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(5) };
        var filtrar = new Button { Text = "Aplicar filtro" }; filtrar.Click += async (_, _) => await FiltrarAsync();
        var todos = new Button { Text = "Mostrar todos" }; todos.Click += async (_, _) => await CargarAsync();
        var venta = new Button { Text = "Comprobante venta" }; venta.Click += async (_, _) => await ComprobanteAsync(false);
        var devolucion = new Button { Text = "Comprobante devolución" }; devolucion.Click += async (_, _) => await ComprobanteAsync(true);
        bar.Controls.AddRange(new Control[] { _filtro, filtrar, todos, venta, devolucion, _estado });
        pagePedidos.Controls.Add(_pedidos); pagePedidos.Controls.Add(bar); _tabs.TabPages.Add(pagePedidos);
        Controls.Add(_tabs);
    }

    private async Task CargarAsync(CancellationToken ct = default)
    {
        try
        {
            _pedidos.DataSource = (await _reportes.ListarPedidosAsync(ct)).ToList();
            await ReemplazarPaginaAsync("Inventario", await _reportes.ListarMovimientosAsync(ct));
            await ReemplazarPaginaAsync("Pagos", await _reportes.ListarPagosAsync(ct));
            await ReemplazarPaginaAsync("Despachos", await _reportes.ListarDespachosAsync(ct));
            await ReemplazarPaginaAsync("Devoluciones", await _reportes.ListarDevolucionesAsync(ct));
            await ReemplazarPaginaAsync("Auditoría", await _reportes.ListarAuditoriaAsync(200, ct));
            var m = _reportes.ObtenerMetricas();
            await ReemplazarPaginaAsync("Métricas", new[] { m });
            _estado.Text = "Datos actualizados";
        }
        catch (Exception ex) { _estado.Text = ex.Message; }
    }

    private async Task FiltrarAsync()
    {
        try { _pedidos.DataSource = (await _reportes.FiltrarPedidosAsync(_filtro.Text)).ToList(); _estado.Text = "Filtro aplicado"; }
        catch (Exception ex) { _estado.Text = ex.Message; }
    }

    private async Task ComprobanteAsync(bool esDevolucion)
    {
        if (_pedidos.CurrentRow?.Cells[nameof(Application.DTOs.PedidoResumenDto.Id)].Value is not int id) return;
        var resultado = esDevolucion
            ? await _comprobantes.EmitirDevolucionAsync(id, "VISTA_PREVIA")
            : await _comprobantes.EmitirVentaAsync(id, "VISTA_PREVIA");
        MessageBox.Show(resultado.Contenido ?? resultado.Mensaje, resultado.Mensaje, MessageBoxButtons.OK,
            resultado.Exitoso ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }

    private Task ReemplazarPaginaAsync<T>(string titulo, IReadOnlyList<T> datos)
    {
        var existente = _tabs.TabPages.Cast<TabPage>().FirstOrDefault(p => p.Text == titulo);
        if (existente is not null) _tabs.TabPages.Remove(existente);
        var page = new TabPage(titulo); var grid = Grid(); grid.DataSource = datos.ToList(); page.Controls.Add(grid); _tabs.TabPages.Add(page);
        return Task.CompletedTask;
    }

    private static DataGridView Grid() => new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false };
}
