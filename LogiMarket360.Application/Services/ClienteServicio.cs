namespace LogiMarket360.Application.Services;

using LogiMarket360.Application.DTOs;
using LogiMarket360.Application.Ports;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Application.Security;
using LogiMarket360.Domain.Entities;
using LogiMarket360.Domain.Enums;

public class ClienteServicio
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAutorizador _autorizador;

    public ClienteServicio(
        IClienteRepository clienteRepository,
        IUnitOfWork unitOfWork,
        IAutorizador autorizador)
    {
        _clienteRepository = clienteRepository ?? throw new ArgumentNullException(nameof(clienteRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _autorizador = autorizador ?? throw new ArgumentNullException(nameof(autorizador));
    }

    public async Task<IReadOnlyList<ClienteDto>> ListarAsync(CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Consulta);
        var clientes = await _clienteRepository.ListarTodosAsync(ct);
        return clientes.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<ClienteDto>> BuscarAsync(string criterio, CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Consulta);
        var clientes = await _clienteRepository.ListarTodosAsync(ct);

        if (string.IsNullOrWhiteSpace(criterio))
            return clientes.Select(ToDto).ToList();

        string c = criterio.Trim();
        return clientes
            .Where(x => x.Nombre.Contains(c, StringComparison.OrdinalIgnoreCase)
                     || x.Identificacion.Contains(c, StringComparison.OrdinalIgnoreCase))
            .Select(ToDto)
            .ToList();
    }

    public async Task<ResultadoOperacion> CrearAsync(
        string nombre,
        string identificacion,
        CategoriaCliente categoria,
        CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Ventas);

        var existente = await _clienteRepository.ObtenerPorIdentificacionAsync(identificacion, ct);
        if (existente is not null)
            return new ResultadoOperacion(false, $"Ya existe un cliente con identificación '{identificacion}'.");

        var cliente = new Cliente(nombre, identificacion, categoria);
        await _clienteRepository.AgregarAsync(cliente, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new ResultadoOperacion(true, $"Cliente '{cliente.Nombre}' creado.", cliente.Id);
    }

    public async Task<ResultadoOperacion> ActualizarAsync(
        int id,
        string nombre,
        string identificacion,
        CategoriaCliente categoria,
        CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Ventas);

        var cliente = await _clienteRepository.ObtenerPorIdAsync(id, ct);
        if (cliente is null)
            return new ResultadoOperacion(false, "Cliente no encontrado.");

        var porIdentificacion = await _clienteRepository.ObtenerPorIdentificacionAsync(identificacion, ct);
        if (porIdentificacion is not null && porIdentificacion.Id != id)
            return new ResultadoOperacion(false, $"La identificación '{identificacion}' ya pertenece a otro cliente.");

        cliente.ActualizarDatos(nombre, identificacion);
        cliente.ActualizarCategoria(categoria);
        _clienteRepository.Actualizar(cliente);
        await _unitOfWork.SaveChangesAsync(ct);

        return new ResultadoOperacion(true, $"Cliente '{cliente.Nombre}' actualizado.", cliente.Id);
    }

    public async Task<ResultadoOperacion> CambiarEstadoAsync(int id, bool activo, CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Ventas);

        var cliente = await _clienteRepository.ObtenerPorIdAsync(id, ct);
        if (cliente is null)
            return new ResultadoOperacion(false, "Cliente no encontrado.");

        if (activo) cliente.Activar();
        else cliente.Desactivar();

        _clienteRepository.Actualizar(cliente);
        await _unitOfWork.SaveChangesAsync(ct);

        return new ResultadoOperacion(true, $"Cliente '{(activo ? "activado" : "desactivado")}' correctamente.", cliente.Id);
    }

    private static ClienteDto ToDto(Cliente c) =>
        new(c.Id, c.Nombre, c.Identificacion, c.Categoria, c.Activo);
}
