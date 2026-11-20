namespace LogiMarket360.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using LogiMarket360.Domain.Entities;
using LogiMarket360.Domain.Enums;

public static class DatabaseSeeder
{
    public static async Task InicializarAsync(LogiMarketDbContext context, bool reiniciarDatos = false)
    {
        if (reiniciarDatos)
        {
            await context.Database.EnsureDeletedAsync();
        }

        await context.Database.EnsureCreatedAsync();

        // 1. Usuarios de prueba con hashing PBKDF2/SHA256
        if (!await context.Usuarios.AnyAsync())
        {
            var usuarios = new List<Usuario>
            {
                new("admin", "admin123", RolUsuario.ADMIN),
                new("ventas", "ventas123", RolUsuario.VENTAS),
                new("bodega", "bodega123", RolUsuario.BODEGA),
                new("consulta", "consulta123", RolUsuario.CONSULTA)
            };
            await context.Usuarios.AddRangeAsync(usuarios);
        }

        // 2. Clientes oficiales de prueba
        if (!await context.Clientes.AnyAsync())
        {
            var clientes = new List<Cliente>
            {
                new("Ana", "CLI-001", CategoriaCliente.VIP),
                new("Luis", "CLI-002", CategoriaCliente.NORMAL),
                new("Carmen", "CLI-003", CategoriaCliente.NORMAL) // Aplica MAYORISTA por volumen de unidades
            };
            await context.Clientes.AddRangeAsync(clientes);
        }

        // 3. Catálogo oficial de productos
        if (!await context.Productos.AnyAsync())
        {
            var productos = new List<Producto>
            {
                new("P01", "Mouse USB", 20.00m, 8, 0.20m),
                new("P02", "Teclado", 30.00m, 5, 0.70m),
                new("P03", "Memoria USB", 10.00m, 20, 0.10m),
                new("P04", "Webcam", 40.00m, 4, 0.35m),
                new("P05", "Alfombrilla", 5.00m, 30, 0.10m)
            };
            await context.Productos.AddRangeAsync(productos);
        }

        // 4. Kits obligatorios
        if (!await context.Kits.AnyAsync())
        {
            var kitOficina = new Kit("KIT_OFICINA", "Kit Oficina", "Incluye Mouse USB y Teclado");
            kitOficina.AgregarComponente("PRODUCTO", "P01", 1);
            kitOficina.AgregarComponente("PRODUCTO", "P02", 1);

            var kitTeletrabajo = new Kit("KIT_TELETRABAJO", "Kit Teletrabajo", "Incluye Kit Oficina y Memoria USB");
            kitTeletrabajo.AgregarComponente("KIT", "KIT_OFICINA", 1);
            kitTeletrabajo.AgregarComponente("PRODUCTO", "P03", 1);

            await context.Kits.AddRangeAsync(kitOficina, kitTeletrabajo);
        }

        await context.SaveChangesAsync();
    }
}
