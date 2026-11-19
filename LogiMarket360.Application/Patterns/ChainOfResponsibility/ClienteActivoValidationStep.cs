namespace LogiMarket360.Application.Patterns.ChainOfResponsibility;

using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Domain.Entities;

public class ClienteActivoValidationStep : ValidationStepBase
{
    private readonly IClienteRepository _clienteRepository;

    public ClienteActivoValidationStep(IClienteRepository clienteRepository)
    {
        _clienteRepository = clienteRepository ?? throw new ArgumentNullException(nameof(clienteRepository));
    }

    protected override async Task<ValidationResult> ExecuteStepAsync(Pedido pedido, CancellationToken ct)
    {
        if (pedido.ClienteId <= 0)
        {
            return ValidationResult.Invalido("ClienteActivo", "El pedido no tiene un cliente asignado.");
        }

        var cliente = await _clienteRepository.ObtenerPorIdAsync(pedido.ClienteId, ct);
        if (cliente == null)
        {
            return ValidationResult.Invalido("ClienteActivo", $"El cliente con ID '{pedido.ClienteId}' no existe en el sistema.");
        }

        if (!cliente.Activo)
        {
            return ValidationResult.Invalido("ClienteActivo", $"El cliente '{cliente.Nombre}' se encuentra inactivo.");
        }

        return ValidationResult.Valido();
    }
}
