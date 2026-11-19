namespace LogiMarket360.Application.Ports.Repositories;

using LogiMarket360.Domain.Entities;

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorUsernameAsync(string username, CancellationToken ct = default);
    Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Usuario>> ListarTodosAsync(CancellationToken ct = default);
    Task AgregarAsync(Usuario usuario, CancellationToken ct = default);
    void Actualizar(Usuario usuario);
}
