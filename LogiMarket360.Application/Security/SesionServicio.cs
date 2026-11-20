namespace LogiMarket360.Application.Security;

using LogiMarket360.Domain.Enums;

public record UsuarioSesion(int Id, string Username, RolUsuario Rol);

public interface ISesionActual
{
    UsuarioSesion? Usuario { get; }
    RolUsuario? Rol { get; }
    bool EstaAutenticada { get; }
}

public interface IAutorizador
{
    bool TieneRol(params RolUsuario[] rolesPermitidos);
    void ExigirRol(params RolUsuario[] rolesPermitidos);
}

public static class Permisos
{
    public static readonly RolUsuario[] Administracion = { RolUsuario.ADMIN };
    public static readonly RolUsuario[] Cobro = { RolUsuario.ADMIN, RolUsuario.VENTAS };
    public static readonly RolUsuario[] Ventas = { RolUsuario.ADMIN, RolUsuario.VENTAS };
    public static readonly RolUsuario[] Despacho = { RolUsuario.ADMIN, RolUsuario.BODEGA };
    public static readonly RolUsuario[] Devolucion = { RolUsuario.ADMIN, RolUsuario.VENTAS };
    public static readonly RolUsuario[] Consulta = { RolUsuario.ADMIN, RolUsuario.VENTAS, RolUsuario.BODEGA, RolUsuario.CONSULTA };
}

public class SesionServicio : ISesionActual, IAutorizador
{
    private readonly object _lock = new();
    private UsuarioSesion? _usuario;

    public UsuarioSesion? Usuario
    {
        get { lock (_lock) return _usuario; }
    }

    public RolUsuario? Rol
    {
        get { lock (_lock) return _usuario?.Rol; }
    }

    public bool EstaAutenticada
    {
        get { lock (_lock) return _usuario is not null; }
    }

    public void Iniciar(UsuarioSesion usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        lock (_lock) _usuario = usuario;
    }

    public void Cerrar()
    {
        lock (_lock) _usuario = null;
    }

    public bool TieneRol(params RolUsuario[] rolesPermitidos)
    {
        if (rolesPermitidos is null || rolesPermitidos.Length == 0)
            return false;

        UsuarioSesion? actual;
        lock (_lock) actual = _usuario;

        return actual is not null && rolesPermitidos.Contains(actual.Rol);
    }

    public void ExigirRol(params RolUsuario[] rolesPermitidos)
    {
        UsuarioSesion? actual;
        lock (_lock) actual = _usuario;

        if (actual is null)
            throw new UnauthorizedAccessException("Acceso denegado: no existe una sesión autenticada.");

        if (!rolesPermitidos.Contains(actual.Rol))
        {
            string requeridos = string.Join(", ", rolesPermitidos.Select(r => r.ToString()));
            throw new UnauthorizedAccessException(
                $"Acceso denegado: el rol '{actual.Rol}' no está autorizado. Roles permitidos: {requeridos}.");
        }
    }
}
