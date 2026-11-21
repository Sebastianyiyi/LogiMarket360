namespace LogiMarket360.WinForms.Views;

using LogiMarket360.Application.DTOs;
using LogiMarket360.Application.Patterns.Mediator;
using LogiMarket360.Application.Services;
using Microsoft.Extensions.DependencyInjection;

public sealed class CatalogoControl : UserControl, IComponenteVista
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IFormularioPrincipalMediator _mediator;
    private readonly DataGridView _productos = CrearGrid();
    private readonly DataGridView _kits = CrearGrid();
    private readonly TextBox _buscar = new() { PlaceholderText = "Buscar por SKU o nombre", Width = 260 };
    private readonly TextBox _sku = new() { PlaceholderText = "SKU", Width = 90 };
    private readonly TextBox _nombre = new() { PlaceholderText = "Nombre", Width = 180 };
    private readonly NumericUpDown _precio = new() { DecimalPlaces = 2, Maximum = 1_000_000, Width = 100 };
    private readonly NumericUpDown _stock = new() { Maximum = 1_000_000, Width = 80 };
    private readonly NumericUpDown _peso = new() { DecimalPlaces = 3, Maximum = 10_000, Width = 90 };
    private readonly Label _estado = new() { AutoSize = true, ForeColor = Color.DimGray };
    private int? _productoId;

    public CatalogoControl(IServiceScopeFactory scopeFactory, IFormularioPrincipalMediator mediator)
    {
        _scopeFactory = scopeFactory;
        _mediator = mediator;
        Dock = DockStyle.Fill;
        Construir();
        _mediator.Registrar(this);
    }

    public AreaVista Area => AreaVista.Catalogo;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _ = CargarAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _mediator.Desregistrar(this);
        base.Dispose(disposing);
    }

    public async Task ActualizarAsync(CambioAplicacion cambio, CancellationToken ct = default) => await CargarAsync(ct);

    private void Construir()
    {
        var herramientas = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(6) };
        var buscar = new Button { Text = "Buscar" };
        buscar.Click += async (_, _) => await CargarAsync();
        herramientas.Controls.AddRange(new Control[] { _buscar, buscar });

        var editor = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 82, Padding = new Padding(6), AutoScroll = true };
        var nuevo = new Button { Text = "Nuevo" };
        nuevo.Click += (_, _) => LimpiarEditor();
        var guardar = new Button { Text = "Guardar" };
        guardar.Click += async (_, _) => await GuardarAsync();
        var ajustar = new Button { Text = "Ajustar stock" };
        ajustar.Click += async (_, _) => await AjustarStockAsync();
        editor.Controls.AddRange(new Control[] { _sku, _nombre, _precio, _stock, _peso, nuevo, guardar, ajustar, _estado });

        _productos.SelectionChanged += (_, _) => SeleccionarProducto();
        var tabs = new TabControl { Dock = DockStyle.Fill };
        var tabProductos = new TabPage("Productos");
        tabProductos.Controls.Add(_productos);
        var tabKits = new TabPage("Kits anidados");
        tabKits.Controls.Add(_kits);
        tabs.TabPages.Add(tabProductos);
        tabs.TabPages.Add(tabKits);
        Controls.Add(tabs);
        Controls.Add(editor);
        Controls.Add(herramientas);
    }

    private async Task CargarAsync(CancellationToken ct = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var servicio = scope.ServiceProvider.GetRequiredService<CatalogoServicio>();
            var productos = await servicio.BuscarProductosAsync(_buscar.Text, ct);
            var kits = await servicio.ListarKitsAsync(ct);
            _productos.DataSource = productos.ToList();
            _kits.DataSource = kits.Select(k => new { k.Codigo, k.Nombre, k.Precio, k.PesoKg, Componentes = k.Componentes.Count }).ToList();
            _estado.Text = $"{productos.Count} productos · {kits.Count} kits";
        }
        catch (Exception ex) { _estado.Text = ex.Message; }
    }

    private async Task GuardarAsync()
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var servicio = scope.ServiceProvider.GetRequiredService<CatalogoServicio>();
            var resultado = _productoId is null
                ? await servicio.CrearProductoAsync(_sku.Text, _nombre.Text, _precio.Value, (int)_stock.Value, _peso.Value)
                : await servicio.ActualizarProductoAsync(_productoId.Value, _nombre.Text, _precio.Value, _peso.Value);
            _estado.Text = resultado.Mensaje;
            if (resultado.Exitoso)
            {
                await CargarAsync();
                await _mediator.NotificarAsync(this, new CambioAplicacion("CatalogoActualizado", new[] { AreaVista.Carrito }));
            }
        }
        catch (Exception ex) { _estado.Text = ex.Message; }
    }

    private async Task AjustarStockAsync()
    {
        if (_productoId is null) { _estado.Text = "Seleccione un producto."; return; }
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var servicio = scope.ServiceProvider.GetRequiredService<CatalogoServicio>();
            var resultado = await servicio.AjustarStockAsync(_productoId.Value, (int)_stock.Value, "Ajuste desde catálogo", _sku.Text);
            _estado.Text = resultado.Mensaje;
            if (resultado.Exitoso) await CargarAsync();
        }
        catch (Exception ex) { _estado.Text = ex.Message; }
    }

    private void SeleccionarProducto()
    {
        if (_productos.CurrentRow?.DataBoundItem is not ProductoDto producto) return;
        _productoId = producto.Id;
        _sku.Text = producto.Sku;
        _sku.ReadOnly = true;
        _nombre.Text = producto.Nombre;
        _precio.Value = producto.Precio;
        _stock.Value = producto.Stock;
        _peso.Value = producto.PesoKg;
    }

    private void LimpiarEditor()
    {
        _productoId = null;
        _sku.ReadOnly = false;
        _sku.Clear();
        _nombre.Clear();
        _precio.Value = _stock.Value = _peso.Value = 0;
    }

    private static DataGridView CrearGrid() => new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AutoGenerateColumns = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false
    };
}
