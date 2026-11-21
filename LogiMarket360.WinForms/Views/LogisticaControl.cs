namespace LogiMarket360.WinForms.Views;

using LogiMarket360.Application.DTOs;
using LogiMarket360.Application.Patterns.Command;
using LogiMarket360.Application.Patterns.Mediator;
using LogiMarket360.Application.Services;
using LogiMarket360.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

public sealed class LogisticaControl : UserControl, IComponenteVista
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IFormularioPrincipalMediator _mediator;
    private readonly DataGridView _grid = Grid();
    private readonly TextBox _proveedor = new() { Text = "LOGISTICA_SIMULADA", Width = 180 };
    private readonly Label _estado = new() { AutoSize = true };

    public LogisticaControl(IServiceScopeFactory scopeFactory, IFormularioPrincipalMediator mediator)
    {
        _scopeFactory = scopeFactory; _mediator = mediator;
        Dock = DockStyle.Fill;
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(5) };
        var refresh = new Button { Text = "Refrescar" }; refresh.Click += async (_, _) => await CargarAsync();
        var dispatch = new Button { Text = "Despachar" }; dispatch.Click += async (_, _) => await DespacharAsync();
        var deliver = new Button { Text = "Marcar entregado" }; deliver.Click += async (_, _) => await EntregarAsync();
        bar.Controls.AddRange(new Control[] { _proveedor, refresh, dispatch, deliver, _estado });
        Controls.Add(_grid); Controls.Add(bar);
        _mediator.Registrar(this);
    }

    public AreaVista Area => AreaVista.Pedidos;
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); _ = CargarAsync(); }
    protected override void Dispose(bool disposing) { if (disposing) _mediator.Desregistrar(this); base.Dispose(disposing); }
    public Task ActualizarAsync(CambioAplicacion cambio, CancellationToken ct = default) => CargarAsync(ct);

    private async Task CargarAsync(CancellationToken ct = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var reportes = scope.ServiceProvider.GetRequiredService<ReporteServicio>();
            var pedidos = await reportes.ListarPedidosAsync(ct);
            var despachos = await reportes.ListarDespachosAsync(ct);
            _grid.DataSource = pedidos.Where(p => p.Estado is EstadoPedido.PAGADO or EstadoPedido.DESPACHADO or EstadoPedido.ENTREGADO)
                .Select(p => new Fila(p.Id, p.NumeroPedido, p.Cliente, p.Estado, p.TipoEntrega,
                    despachos.FirstOrDefault(d => d.PedidoId == p.Id)?.NumeroGuia ?? "",
                    despachos.FirstOrDefault(d => d.PedidoId == p.Id)?.FechaEntregaEstimada)).ToList();
            _estado.Text = $"{_grid.RowCount} operaciones";
        }
        catch (Exception ex) { _estado.Text = ex.Message; }
    }

    private async Task DespacharAsync()
    {
        if (_grid.CurrentRow?.DataBoundItem is not Fila fila) return;
        await using var scope = _scopeFactory.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<ICommandFactory>();
        var result = await scope.ServiceProvider.GetRequiredService<ICommandInvoker>()
            .InvocarAsync(factory.CrearDespachar(fila.Id, null, _proveedor.Text));
        _estado.Text = result.Mensaje; await CargarAsync();
        if (result.Exitoso) await _mediator.NotificarAsync(this, new CambioAplicacion("PedidoDespachado", new[] { AreaVista.Carrito, AreaVista.Metricas }));
    }

    private async Task EntregarAsync()
    {
        if (_grid.CurrentRow?.DataBoundItem is not Fila fila) return;
        await using var scope = _scopeFactory.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<ICommandFactory>();
        var result = await scope.ServiceProvider.GetRequiredService<ICommandInvoker>()
            .InvocarAsync(factory.CrearEntregar(fila.Id));
        _estado.Text = result.Mensaje; await CargarAsync();
        if (result.Exitoso) await _mediator.NotificarAsync(this, new CambioAplicacion("PedidoEntregado", new[] { AreaVista.Carrito }));
    }

    private static DataGridView Grid() => new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false };
    private sealed record Fila(int Id, string Pedido, string Cliente, EstadoPedido Estado, TipoEntrega Entrega, string Guia, DateTime? EntregaEstimada);
}
