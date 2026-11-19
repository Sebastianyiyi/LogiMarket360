namespace LogiMarket360.Application.DTOs;

public record ResultadoPago(
    bool Exitoso,
    string ReferenciaTransaccion,
    string Mensaje,
    decimal MontoCobrado,
    string Proveedor
);
