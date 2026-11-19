namespace LogiMarket360.Application.Patterns.Observer.Eventos;

public record PedidoPagadoEvent(
    int PedidoId,
    string NumeroPedido,
    int ClienteId,
    decimal TotalPagado,
    string ReferenciaPago,
    DateTime FechaPago
);

public record DespachoRegistradoEvent(
    int PedidoId,
    string NumeroGuia,
    string ProveedorLogistico,
    DateTime FechaDespacho
);

public record DevolucionAceptadaEvent(
    int PedidoId,
    decimal MontoReembolsado,
    string Motivo,
    DateTime FechaDevolucion
);
