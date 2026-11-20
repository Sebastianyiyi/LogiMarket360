namespace LogiMarket360.Application.Services;

using LogiMarket360.Application.DTOs;
using LogiMarket360.Application.Ports;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Application.Security;
using LogiMarket360.Domain.Entities;
using LogiMarket360.Domain.Enums;
using LogiMarket360.Domain.Exceptions;
using LogiMarket360.Domain.Patterns.Composite;

public class CatalogoServicio
{
    private readonly IProductoRepository _productoRepository;
    private readonly IKitRepository _kitRepository;
    private readonly IMovimientoInventarioRepository _movimientoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAutorizador _autorizador;

    public CatalogoServicio(
        IProductoRepository productoRepository,
        IKitRepository kitRepository,
        IMovimientoInventarioRepository movimientoRepository,
        IUnitOfWork unitOfWork,
        IAutorizador autorizador)
    {
        _productoRepository = productoRepository ?? throw new ArgumentNullException(nameof(productoRepository));
        _kitRepository = kitRepository ?? throw new ArgumentNullException(nameof(kitRepository));
        _movimientoRepository = movimientoRepository ?? throw new ArgumentNullException(nameof(movimientoRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _autorizador = autorizador ?? throw new ArgumentNullException(nameof(autorizador));
    }

    public async Task<IReadOnlyList<ProductoDto>> ListarProductosAsync(CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Consulta);
        var productos = await _productoRepository.ListarTodosAsync(ct);
        return productos.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<ProductoDto>> BuscarProductosAsync(string criterio, CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Consulta);
        if (string.IsNullOrWhiteSpace(criterio))
            return await ListarProductosAsync(ct);

        var productos = await _productoRepository.BuscarAsync(criterio.Trim(), ct);
        return productos.Select(ToDto).ToList();
    }

    public async Task<ResultadoOperacion> CrearProductoAsync(
        string sku,
        string nombre,
        decimal precio,
        int stockInicial,
        decimal pesoKg,
        CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Administracion);

        var existente = await _productoRepository.ObtenerPorSkuAsync(sku, ct);
        if (existente is not null)
            return new ResultadoOperacion(false, $"Ya existe un producto con SKU '{sku}'.");

        var producto = new Producto(sku, nombre, precio, stockInicial, pesoKg);
        await _productoRepository.AgregarAsync(producto, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        if (stockInicial > 0)
        {
            var movimiento = new MovimientoInventario(
                producto.Id,
                producto.Sku,
                stockInicial,
                TipoMovimientoInventario.AJUSTE,
                "Stock inicial de catálogo",
                producto.Sku);
            await _movimientoRepository.AgregarAsync(movimiento, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return new ResultadoOperacion(true, $"Producto '{producto.Sku}' creado.", producto.Id);
    }

    public async Task<ResultadoOperacion> ActualizarProductoAsync(
        int id,
        string nombre,
        decimal precio,
        decimal pesoKg,
        CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Administracion);

        var producto = await _productoRepository.ObtenerPorIdAsync(id, ct);
        if (producto is null)
            return new ResultadoOperacion(false, "Producto no encontrado.");

        producto.ActualizarDatos(nombre, pesoKg);
        producto.ActualizarPrecio(precio);
        _productoRepository.Actualizar(producto);
        await _unitOfWork.SaveChangesAsync(ct);

        return new ResultadoOperacion(true, $"Producto '{producto.Sku}' actualizado.", producto.Id);
    }

    public async Task<ResultadoOperacion> AjustarStockAsync(
        int productoId,
        int nuevoStock,
        string motivo,
        string referencia,
        CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Administracion);

        var producto = await _productoRepository.ObtenerPorIdAsync(productoId, ct);
        if (producto is null)
            return new ResultadoOperacion(false, "Producto no encontrado.");

        int diferencia = nuevoStock - producto.Stock;
        if (diferencia == 0)
            return new ResultadoOperacion(true, "El stock no cambió.");

        producto.AjustarStock(nuevoStock);
        _productoRepository.Actualizar(producto);

        var movimiento = new MovimientoInventario(
            producto.Id,
            producto.Sku,
            diferencia,
            TipoMovimientoInventario.AJUSTE,
            string.IsNullOrWhiteSpace(motivo) ? "Ajuste manual de inventario" : motivo,
            string.IsNullOrWhiteSpace(referencia) ? producto.Sku : referencia);

        await _movimientoRepository.AgregarAsync(movimiento, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new ResultadoOperacion(true, $"Stock de '{producto.Sku}' ajustado a {producto.Stock}.");
    }

    public async Task<ResultadoOperacion> EliminarProductoAsync(int id, CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Administracion);

        var producto = await _productoRepository.ObtenerPorIdAsync(id, ct);
        if (producto is null)
            return new ResultadoOperacion(false, "Producto no encontrado.");

        var movimientos = await _movimientoRepository.ListarPorProductoAsync(id, ct);
        if (movimientos.Count > 0)
            return new ResultadoOperacion(false, "No se puede eliminar un producto con movimientos de inventario.");

        var kits = await _kitRepository.ListarTodosAsync(ct);
        bool estaEnKit = kits.Any(k => k.Componentes.Any(c =>
            c.TipoElemento == "PRODUCTO" &&
            c.ElementoCodigo.Equals(producto.Sku, StringComparison.OrdinalIgnoreCase)));

        if (estaEnKit)
            return new ResultadoOperacion(false, "No se puede eliminar un producto que pertenece a un kit.");

        _productoRepository.Eliminar(producto);
        await _unitOfWork.SaveChangesAsync(ct);
        return new ResultadoOperacion(true, $"Producto '{producto.Sku}' eliminado.");
    }

    public async Task<IReadOnlyList<KitDto>> ListarKitsAsync(CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Consulta);

        var productos = await _productoRepository.ListarTodosAsync(ct);
        var kits = await _kitRepository.ListarTodosAsync(ct);
        var productoPorSku = productos.ToDictionary(p => p.Sku, StringComparer.OrdinalIgnoreCase);
        var kitPorCodigo = kits.ToDictionary(k => k.CodigoKit, StringComparer.OrdinalIgnoreCase);

        var resultado = new List<KitDto>();
        foreach (var kit in kits)
        {
            var arbol = ConstruirArbol(kit, productoPorSku, kitPorCodigo, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            var componentes = kit.Componentes
                .Select(c => new ComponenteKitDto(
                    c.ElementoCodigo,
                    ObtenerNombreComponente(c, productoPorSku, kitPorCodigo),
                    c.Cantidad,
                    c.TipoElemento == "KIT"))
                .ToList();

            resultado.Add(new KitDto(
                kit.CodigoKit,
                kit.Nombre,
                kit.Descripcion,
                arbol.ObtenerPrecio(),
                arbol.ObtenerPeso(),
                componentes));
        }

        return resultado;
    }

    public async Task<KitCompuesto> ObtenerArbolKitAsync(string codigoKit, CancellationToken ct = default)
    {
        var productos = await _productoRepository.ListarTodosAsync(ct);
        var kits = await _kitRepository.ListarTodosAsync(ct);
        var productoPorSku = productos.ToDictionary(p => p.Sku, StringComparer.OrdinalIgnoreCase);
        var kitPorCodigo = kits.ToDictionary(k => k.CodigoKit, StringComparer.OrdinalIgnoreCase);

        if (!kitPorCodigo.TryGetValue(codigoKit, out var kit))
            throw new DomainException($"El kit '{codigoKit}' no existe.");

        return ConstruirArbol(kit, productoPorSku, kitPorCodigo, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    public async Task<IElementoCatalogo> ObtenerElementoAsync(string codigo, bool esKit, CancellationToken ct = default)
    {
        if (esKit)
            return await ObtenerArbolKitAsync(codigo, ct);

        var producto = await _productoRepository.ObtenerPorSkuAsync(codigo, ct)
            ?? throw new DomainException($"El producto '{codigo}' no existe.");

        return new ProductoSimple(producto.Sku, producto.Nombre, producto.Precio, producto.PesoKg);
    }

    public async Task<ResultadoOperacion> CrearKitAsync(
        string codigoKit,
        string nombre,
        string descripcion,
        IReadOnlyList<ComponenteKitSolicitud> componentes,
        CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Administracion);

        if (componentes is null || componentes.Count == 0)
            return new ResultadoOperacion(false, "El kit debe tener al menos un componente.");

        string codigoNormalizado = codigoKit.Trim().ToUpperInvariant();
        var existente = await _kitRepository.ObtenerPorCodigoAsync(codigoNormalizado, ct);
        if (existente is not null)
            return new ResultadoOperacion(false, $"Ya existe un kit con código '{codigoNormalizado}'.");

        var productos = await _productoRepository.ListarTodosAsync(ct);
        var kits = await _kitRepository.ListarTodosAsync(ct);
        var productoPorSku = productos.ToDictionary(p => p.Sku, StringComparer.OrdinalIgnoreCase);
        var kitPorCodigo = kits.ToDictionary(k => k.CodigoKit, StringComparer.OrdinalIgnoreCase);

        var nuevoKit = new KitCompuesto(codigoNormalizado, nombre);
        foreach (var componente in componentes)
        {
            if (componente.Cantidad <= 0)
                return new ResultadoOperacion(false, $"La cantidad del componente '{componente.Codigo}' debe ser mayor a cero.");

            if (componente.EsKit)
            {
                if (!kitPorCodigo.TryGetValue(componente.Codigo, out var kitHijo))
                    return new ResultadoOperacion(false, $"El kit '{componente.Codigo}' no existe.");

                var arbolHijo = ConstruirArbol(kitHijo, productoPorSku, kitPorCodigo, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                nuevoKit.AgregarComponente(arbolHijo, componente.Cantidad);
            }
            else
            {
                if (!productoPorSku.TryGetValue(componente.Codigo, out var producto))
                    return new ResultadoOperacion(false, $"El producto '{componente.Codigo}' no existe.");

                nuevoKit.AgregarComponente(
                    new ProductoSimple(producto.Sku, producto.Nombre, producto.Precio, producto.PesoKg),
                    componente.Cantidad);
            }
        }

        // Fuerza el cálculo recursivo para detectar ciclos antes de persistir.
        _ = nuevoKit.ObtenerPrecio();
        _ = nuevoKit.ObtenerPeso();

        var kit = new Kit(codigoNormalizado, nombre, descripcion);
        foreach (var componente in componentes)
        {
            kit.AgregarComponente(
                componente.EsKit ? "KIT" : "PRODUCTO",
                componente.Codigo,
                componente.Cantidad);
        }

        await _kitRepository.AgregarAsync(kit, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new ResultadoOperacion(true, $"Kit '{codigoNormalizado}' creado.", kit.Id);
    }

    public async Task<ResultadoOperacion> EliminarKitAsync(string codigoKit, CancellationToken ct = default)
    {
        _autorizador.ExigirRol(Permisos.Administracion);

        var kit = await _kitRepository.ObtenerPorCodigoAsync(codigoKit, ct);
        if (kit is null)
            return new ResultadoOperacion(false, "Kit no encontrado.");

        var todos = await _kitRepository.ListarTodosAsync(ct);
        bool referenciado = todos.Any(k => !k.CodigoKit.Equals(kit.CodigoKit, StringComparison.OrdinalIgnoreCase)
            && k.Componentes.Any(c => c.TipoElemento == "KIT" &&
                c.ElementoCodigo.Equals(kit.CodigoKit, StringComparison.OrdinalIgnoreCase)));

        if (referenciado)
            return new ResultadoOperacion(false, "No se puede eliminar un kit que es componente de otro kit.");

        _kitRepository.Eliminar(kit);
        await _unitOfWork.SaveChangesAsync(ct);
        return new ResultadoOperacion(true, $"Kit '{kit.CodigoKit}' eliminado.");
    }

    private static KitCompuesto ConstruirArbol(
        Kit kit,
        IReadOnlyDictionary<string, Producto> productoPorSku,
        IReadOnlyDictionary<string, Kit> kitPorCodigo,
        HashSet<string> visitados)
    {
        if (!visitados.Add(kit.CodigoKit))
            throw new CicloDetectadoException(kit.CodigoKit, "Se detectó un ciclo al construir la composición del kit.");

        var compuesto = new KitCompuesto(kit.CodigoKit, kit.Nombre);

        foreach (var componente in kit.Componentes)
        {
            if (componente.TipoElemento.Equals("KIT", StringComparison.OrdinalIgnoreCase))
            {
                if (!kitPorCodigo.TryGetValue(componente.ElementoCodigo, out var kitHijo))
                    throw new DomainException($"El kit '{kit.CodigoKit}' referencia al kit inexistente '{componente.ElementoCodigo}'.");

                var arbolHijo = ConstruirArbol(kitHijo, productoPorSku, kitPorCodigo, new HashSet<string>(visitados, StringComparer.OrdinalIgnoreCase));
                compuesto.AgregarComponente(arbolHijo, componente.Cantidad);
            }
            else
            {
                if (!productoPorSku.TryGetValue(componente.ElementoCodigo, out var producto))
                    throw new DomainException($"El kit '{kit.CodigoKit}' referencia al producto inexistente '{componente.ElementoCodigo}'.");

                compuesto.AgregarComponente(
                    new ProductoSimple(producto.Sku, producto.Nombre, producto.Precio, producto.PesoKg),
                    componente.Cantidad);
            }
        }

        visitados.Remove(kit.CodigoKit);
        return compuesto;
    }

    private static string ObtenerNombreComponente(
        KitComponente componente,
        IReadOnlyDictionary<string, Producto> productoPorSku,
        IReadOnlyDictionary<string, Kit> kitPorCodigo)
    {
        if (componente.TipoElemento.Equals("KIT", StringComparison.OrdinalIgnoreCase))
            return kitPorCodigo.TryGetValue(componente.ElementoCodigo, out var kit) ? kit.Nombre : componente.ElementoCodigo;

        return productoPorSku.TryGetValue(componente.ElementoCodigo, out var producto) ? producto.Nombre : componente.ElementoCodigo;
    }

    private static ProductoDto ToDto(Producto p) =>
        new(p.Id, p.Sku, p.Nombre, p.Precio, p.Stock, p.PesoKg);
}
