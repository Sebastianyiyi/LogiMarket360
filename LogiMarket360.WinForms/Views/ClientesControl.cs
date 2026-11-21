namespace LogiMarket360.WinForms.Views;

using LogiMarket360.Application.DTOs;
using LogiMarket360.Application.Patterns.Mediator;
using LogiMarket360.Application.Services;
using LogiMarket360.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

public sealed class ClientesControl : UserControl
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false
    };
    private readonly TextBox _buscar = new() { PlaceholderText = "Buscar cliente", Width = 240 };
    private readonly TextBox _nombre = new() { PlaceholderText = "Nombre", Width = 180 };
    private readonly TextBox _identificacion = new() { PlaceholderText = "Identificación", Width = 130 };
    private readonly ComboBox _categoria = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    private readonly Label _estado = new() { AutoSize = true };
    private int? _clienteId;

    public ClientesControl(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        Dock = DockStyle.Fill;
        _categoria.DataSource = Enum.GetValues<CategoriaCliente>();
        Construir();
    }

    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); _ = CargarAsync(); }

    private void Construir()
    {
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(6) };
        var buscar = new Button { Text = "Buscar" };
        buscar.Click += async (_, _) => await CargarAsync();
        top.Controls.AddRange(new Control[] { _buscar, buscar });
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 80, Padding = new Padding(6), AutoScroll = true };
        var nuevo = new Button { Text = "Nuevo" };
        nuevo.Click += (_, _) => Limpiar();
        var guardar = new Button { Text = "Guardar" };
        guardar.Click += async (_, _) => await GuardarAsync();
        var estado = new Button { Text = "Activar/Desactivar" };
        estado.Click += async (_, _) => await AlternarEstadoAsync();
        bottom.Controls.AddRange(new Control[] { _nombre, _identificacion, _categoria, nuevo, guardar, estado, _estado });
        _grid.SelectionChanged += (_, _) => Seleccionar();
        Controls.Add(_grid);
        Controls.Add(bottom);
        Controls.Add(top);
    }

    private async Task CargarAsync()
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var datos = await scope.ServiceProvider.GetRequiredService<ClienteServicio>().BuscarAsync(_buscar.Text);
            _grid.DataSource = datos.ToList();
            _estado.Text = $"{datos.Count} clientes";
        }
        catch (Exception ex) { _estado.Text = ex.Message; }
    }

    private async Task GuardarAsync()
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var servicio = scope.ServiceProvider.GetRequiredService<ClienteServicio>();
            var categoria = (CategoriaCliente)_categoria.SelectedItem!;
            var resultado = _clienteId is null
                ? await servicio.CrearAsync(_nombre.Text, _identificacion.Text, categoria)
                : await servicio.ActualizarAsync(_clienteId.Value, _nombre.Text, _identificacion.Text, categoria);
            _estado.Text = resultado.Mensaje;
            if (resultado.Exitoso) await CargarAsync();
        }
        catch (Exception ex) { _estado.Text = ex.Message; }
    }

    private async Task AlternarEstadoAsync()
    {
        if (_grid.CurrentRow?.DataBoundItem is not ClienteDto cliente) return;
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var resultado = await scope.ServiceProvider.GetRequiredService<ClienteServicio>()
                .CambiarEstadoAsync(cliente.Id, !cliente.Activo);
            _estado.Text = resultado.Mensaje;
            if (resultado.Exitoso) await CargarAsync();
        }
        catch (Exception ex) { _estado.Text = ex.Message; }
    }

    private void Seleccionar()
    {
        if (_grid.CurrentRow?.DataBoundItem is not ClienteDto cliente) return;
        _clienteId = cliente.Id;
        _nombre.Text = cliente.Nombre;
        _identificacion.Text = cliente.Identificacion;
        _categoria.SelectedItem = cliente.Categoria;
    }

    private void Limpiar()
    {
        _clienteId = null;
        _nombre.Clear();
        _identificacion.Clear();
        _categoria.SelectedIndex = 0;
    }
}
