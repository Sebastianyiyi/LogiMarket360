namespace LogiMarket360.Domain.Entities;

using System.Security.Cryptography;
using System.Text;
using LogiMarket360.Domain.Enums;

public class Usuario
{
    public int Id { get; set; }
    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string PasswordSalt { get; private set; } = string.Empty;
    public RolUsuario Rol { get; private set; }
    public bool Activo { get; private set; } = true;

    // Requerido por EF Core
    protected Usuario() { }

    public Usuario(string username, string passwordClara, RolUsuario rol)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("El nombre de usuario no puede ser vacío.", nameof(username));
        if (string.IsNullOrWhiteSpace(passwordClara))
            throw new ArgumentException("La contraseña no puede ser vacía.", nameof(passwordClara));

        Username = username.Trim().ToLowerInvariant();
        Rol = rol;
        Activo = true;

        byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
        PasswordSalt = Convert.ToBase64String(saltBytes);
        PasswordHash = HashPassword(passwordClara, saltBytes);
    }

    public bool VerificarPassword(string passwordClara)
    {
        if (string.IsNullOrWhiteSpace(passwordClara) || !Activo)
            return false;

        byte[] saltBytes = Convert.FromBase64String(PasswordSalt);
        string hashCalculado = HashPassword(passwordClara, saltBytes);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(PasswordHash),
            Encoding.UTF8.GetBytes(hashCalculado));
    }

    private static string HashPassword(string password, byte[] salt)
    {
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password: Encoding.UTF8.GetBytes(password),
            salt: salt,
            iterations: 100_000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: 32);

        return Convert.ToBase64String(hash);
    }

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}
