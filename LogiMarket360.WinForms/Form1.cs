namespace LogiMarket360.WinForms;

using LogiMarket360.Application.Services;
using LogiMarket360.Domain.Enums;
using LogiMarket360.WinForms.Views;

public partial class Form1 : Form
{
    private readonly AutenticacionServicio _autenticacion;
    private readonly CatalogoControl _catalogo;
    private readonly ClientesControl _clientes;
    private readonly PedidosControl _pedidos;
    private readonly LogisticaControl _logistica;
    private readonly DevolucionesControl _devoluciones;
    private readonly ConsultasControl _consultas;
    private readonly Panel _content = new() { Dock = DockStyle.Fill };

    public Form1(
        AutenticacionServicio autenticacion,
        CatalogoControl catalogo,
        ClientesControl clientes,
        PedidosControl pedidos,
        LogisticaControl logistica,
        DevolucionesControl devoluciones,
        ConsultasControl consultas)
    {
        _autenticacion = autenticacion;
        _catalogo = catalogo;
        _clientes = clientes;
        _pedidos = pedidos;
        _logistica = logistica;
        _devoluciones = devoluciones;
        _consultas = consultas;
        InitializeComponent();
        Controls.Add(_content);
        MostrarAcceso();
    }

    private void MostrarAcceso()
    {
        _content.Controls.Clear();

        var card = new TableLayoutPanel
        {
            Anchor = AnchorStyles.None,
            Width = 390,
            Height = 330,
            ColumnCount = 1,
            RowCount = 8,
            Padding = new Padding(32),
            BackColor = Color.White
        };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        card.Controls.Add(new Label
        {
            Text = "LogiMarket 360",
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 62, 93),
            AutoSize = true,
            Anchor = AnchorStyles.None
        });
        card.Controls.Add(new Label
        {
            Text = "Acceso local seguro",
            ForeColor = Color.DimGray,
            AutoSize = true,
            Anchor = AnchorStyles.None
        });

        var username = new TextBox { PlaceholderText = "Usuario", Dock = DockStyle.Fill };
        var password = new TextBox { PlaceholderText = "Contraseña", UseSystemPasswordChar = true, Dock = DockStyle.Fill };
        var feedback = new Label { AutoSize = true, ForeColor = Color.Firebrick, Anchor = AnchorStyles.None };
        var login = new Button
        {
            Text = "Iniciar sesión",
            Dock = DockStyle.Fill,
            Height = 42,
            BackColor = Color.FromArgb(30, 104, 168),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        login.Click += async (_, _) =>
        {
            login.Enabled = false;
            try
            {
                var resultado = await _autenticacion.IniciarSesionAsync(username.Text, password.Text);
                if (!resultado.Exitoso || resultado.Usuario is null)
                {
                    feedback.Text = resultado.Mensaje;
                    return;
                }

                MostrarEspacioTrabajo(resultado.Usuario.Username, resultado.Usuario.Rol);
            }
            finally
            {
                login.Enabled = true;
            }
        };

        card.Controls.Add(username);
        card.Controls.Add(password);
        card.Controls.Add(login);
        card.Controls.Add(feedback);
        card.Controls.Add(new Label
        {
            Text = "Usuarios demo: admin, ventas, bodega, consulta",
            ForeColor = Color.Gray,
            AutoSize = true,
            Anchor = AnchorStyles.None
        });

        var host = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(235, 241, 246) };
        host.Controls.Add(card);
        host.SetCellPosition(card, new TableLayoutPanelCellPosition(0, 0));
        card.Anchor = AnchorStyles.None;
        _content.Controls.Add(host);
        AcceptButton = login;
        username.Focus();
    }

    private void MostrarEspacioTrabajo(string username, RolUsuario rol)
    {
        _content.Controls.Clear();
        AcceptButton = null;

        var header = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = Color.FromArgb(24, 62, 93) };
        header.Controls.Add(new Label
        {
            Text = "LogiMarket 360",
            Font = new Font("Segoe UI", 17, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 16)
        });
        var logout = new Button
        {
            Text = "Cerrar sesión",
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(ClientSize.Width - 130, 16)
        };
        logout.Click += (_, _) =>
        {
            _autenticacion.CerrarSesion();
            MostrarAcceso();
        };
        header.Resize += (_, _) => logout.Left = header.ClientSize.Width - logout.Width - 20;
        header.Controls.Add(logout);

        var session = new Label
        {
            Text = $"Sesión: {username} · {rol}",
            Dock = DockStyle.Bottom,
            Height = 28,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 16, 0),
            BackColor = Color.FromArgb(221, 231, 239)
        };

        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(16, 8) };
        tabs.TabPages.Add(CrearPagina("Catálogo", _catalogo));
        if (rol is RolUsuario.ADMIN or RolUsuario.VENTAS)
        {
            tabs.TabPages.Add(CrearPagina("Clientes", _clientes));
            tabs.TabPages.Add(CrearPagina("Pedidos", _pedidos));
            tabs.TabPages.Add(CrearPagina("Devoluciones", _devoluciones));
        }
        if (rol is RolUsuario.ADMIN or RolUsuario.BODEGA)
            tabs.TabPages.Add(CrearPagina("Logística", _logistica));
        tabs.TabPages.Add(CrearPagina("Consultas", _consultas));

        _content.Controls.Add(tabs);
        _content.Controls.Add(session);
        _content.Controls.Add(header);
    }

    private static TabPage CrearPagina(string titulo, string descripcion)
    {
        var pagina = new TabPage(titulo) { Name = $"tab{titulo}" };
        pagina.Controls.Add(new Label
        {
            Text = descripcion,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 62, 93),
            AutoSize = true,
            Location = new Point(22, 22)
        });
        return pagina;
    }

    private static TabPage CrearPagina(string titulo, Control contenido)
    {
        var pagina = new TabPage(titulo) { Name = $"tab{titulo}" };
        contenido.Dock = DockStyle.Fill;
        pagina.Controls.Add(contenido);
        return pagina;
    }
}
