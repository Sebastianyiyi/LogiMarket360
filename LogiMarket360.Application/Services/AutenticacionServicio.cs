namespace LogiMarket360.Application.Services;

using LogiMarket360.Application.DTOs;
using LogiMarket360.Application.Patterns.Singleton;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Application.Security;

public class AutenticacionServicio
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly SesionServicio _sesion;
    private readonly MetricasSesion _metricas;

    public AutenticacionServicio(
        IUsuarioRepository usuarioRepository,
        SesionServicio sesion,
        MetricasSesion metricas)
    {
        _usuarioRepository = usuarioRepository ?? throw new ArgumentNullException(nameof(usuarioRepository));
        _sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
        _metricas = metricas ?? throw new ArgumentNullException(nameof(metricas));
    }

    public async Task<ResultadoAutenticacion> IniciarSesionAsync(
        string username,
        string password,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return new ResultadoAutenticacion(false, "Debe ingresar usuario y contraseña.");

        var usuario = await _usuarioRepository.ObtenerPorUsernameAsync(username, ct);
        if (usuario is null || !usuario.VerificarPassword(password))
            return new ResultadoAutenticacion(false, "Credenciales inválidas.");

        if (!usuario.Activo)
            return new ResultadoAutenticacion(false, "El usuario se encuentra inactivo.");

        _sesion.Iniciar(new UsuarioSesion(usuario.Id, usuario.Username, usuario.Rol));
        _metricas.Reiniciar();

        return new ResultadoAutenticacion(
            true,
            $"Bienvenido, {usuario.Username} ({usuario.Rol}).",
            new UsuarioSesionDto(usuario.Id, usuario.Username, usuario.Rol));
    }

    public void CerrarSesion()
    {
        _sesion.Cerrar();
    }

    public UsuarioSesionDto? ObtenerSesionActual()
    {
        var actual = _sesion.Usuario;
        return actual is null
            ? null
            : new UsuarioSesionDto(actual.Id, actual.Username, actual.Rol);
    }
}
