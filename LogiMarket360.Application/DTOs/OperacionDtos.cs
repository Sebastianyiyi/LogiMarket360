namespace LogiMarket360.Application.DTOs;

using LogiMarket360.Domain.Enums;

public record UsuarioSesionDto(int Id, string Username, RolUsuario Rol);

public record ResultadoAutenticacion(bool Exitoso, string Mensaje, UsuarioSesionDto? Usuario = null);

public record ResultadoOperacion(bool Exitoso, string Mensaje, int? EntidadId = null);

public record ResultadoComprobante(bool Exitoso, string Mensaje, string? Contenido = null);

public record ProductoDto(
    int Id,
    string Sku,
    string Nombre,
    decimal Precio,
    int Stock,
    decimal PesoKg
);

public record ComponenteKitDto(
    string Codigo,
    string Nombre,
    int Cantidad,
    bool EsKit
);

public record KitDto(
    string Codigo,
    string Nombre,
    string Descripcion,
    decimal Precio,
    decimal PesoKg,
    IReadOnlyList<ComponenteKitDto> Componentes
);

public record ClienteDto(
    int Id,
    string Nombre,
    string Identificacion,
    CategoriaCliente Categoria,
    bool Activo
);

public record LineaPedidoSolicitud(string Codigo, int Cantidad, bool EsKit);

public record ComponenteKitSolicitud(string Codigo, int Cantidad, bool EsKit);

public record PedidoDetalleDto(
    string CodigoItem,
    string NombreItem,
    bool EsKit,
    int Cantidad,
    decimal PrecioUnitario,
    decimal SubtotalLinea,
    IReadOnlyList<ComponentePedidoDto> Componentes
);

public record ComponentePedidoDto(
    string Sku,
    string Nombre,
    int CantidadFisica,
    decimal PrecioUnitario,
    decimal PesoKg
);

public record PedidoResumenDto(
    int Id,
    string NumeroPedido,
    int ClienteId,
    string Cliente,
    CategoriaCliente CategoriaCliente,
    EstadoPedido Estado,
    TipoEntrega TipoEntrega,
    decimal Subtotal,
    decimal Descuento,
    string EstrategiaDescuento,
    decimal CostoEnvio,
    decimal Total,
    DateTime FechaCreacion,
    string IdempotencyKey,
    IReadOnlyList<PedidoDetalleDto> Detalles
);

public record PagoDto(
    int Id,
    int PedidoId,
    decimal Monto,
    string Referencia,
    string ClaveIdempotencia,
    string Proveedor,
    DateTime FechaPago,
    bool Aprobado
);

public record DespachoDto(
    int Id,
    int PedidoId,
    string NumeroGuia,
    string ProveedorLogistico,
    TipoEntrega TipoEntrega,
    int PlazoEstimadoDias,
    DateTime FechaDespacho,
    DateTime FechaEntregaEstimada,
    DateTime? FechaEntregaReal
);

public record DevolucionDto(
    int Id,
    int PedidoId,
    MotivoDevolucion Motivo,
    DateTime FechaSolicitud,
    int DiasDesdeEntrega,
    bool ProductoSinUso,
    bool Aprobada,
    decimal MontoReembolso,
    bool ReintegroStockRealizado,
    string ReferenciaReembolso,
    string DetalleMotivo
);

public record MovimientoInventarioDto(
    int Id,
    int ProductoId,
    string Sku,
    int Cantidad,
    TipoMovimientoInventario TipoMovimiento,
    string Motivo,
    string Referencia,
    DateTime Fecha
);

public record MetricasSesionDto(
    int TotalPedidosConfirmados,
    decimal MontoTotalVentas,
    int TotalDevolucionesAceptadas,
    decimal MontoTotalReembolsos,
    int IntentosCobroFallidos,
    DateTime InicioSesion
);
