namespace LogiMarket360.Domain.Entities;

using LogiMarket360.Domain.Enums;

public class Cliente
{
    public int Id { get; set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Identificacion { get; private set; } = string.Empty;
    public CategoriaCliente Categoria { get; private set; }
    public bool Activo { get; private set; } = true;

    protected Cliente() { }

    public Cliente(string nombre, string identificacion, CategoriaCliente categoria)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del cliente no puede ser vacío.", nameof(nombre));
        if (string.IsNullOrWhiteSpace(identificacion))
            throw new ArgumentException("La identificación no puede ser vacía.", nameof(identificacion));

        Nombre = nombre.Trim();
        Identificacion = identificacion.Trim();
        Categoria = categoria;
        Activo = true;
    }

    public void ActualizarCategoria(CategoriaCliente nuevaCategoria)
    {
        Categoria = nuevaCategoria;
    }

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}
