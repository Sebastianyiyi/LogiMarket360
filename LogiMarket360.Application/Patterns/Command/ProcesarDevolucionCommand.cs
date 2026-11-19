namespace LogiMarket360.Application.Patterns.Command;

using LogiMarket360.Application.Patterns.TemplateMethod;
using LogiMarket360.Domain.Enums;

public class ProcesarDevolucionCommand : ICommand
{
    private readonly SolicitudDevolucion _solicitud;
    private readonly DevolucionDefectoService _servicioDefecto;
    private readonly DevolucionArrepentimientoService _servicioArrepentimiento;

    public string Nombre => "ProcesarDevolucionCommand";

    public ProcesarDevolucionCommand(
        SolicitudDevolucion solicitud,
        DevolucionDefectoService servicioDefecto,
        DevolucionArrepentimientoService servicioArrepentimiento)
    {
        _solicitud = solicitud;
        _servicioDefecto = servicioDefecto;
        _servicioArrepentimiento = servicioArrepentimiento;
    }

    public async Task<CommandResult> ExecuteAsync(CancellationToken ct = default)
    {
        ProcesarDevolucionTemplate template = _solicitud.Motivo switch
        {
            MotivoDevolucion.DEFECTO => _servicioDefecto,
            MotivoDevolucion.ARREPENTIMIENTO => _servicioArrepentimiento,
            _ => throw new ArgumentOutOfRangeException(nameof(_solicitud.Motivo), $"Motivo de devolución no soportado: {_solicitud.Motivo}")
        };

        var resultado = await template.ProcesarAsync(_solicitud, ct);

        if (resultado.Aprobada)
        {
            return CommandResult.Ok(resultado.Mensaje, resultado);
        }

        return CommandResult.Falla(resultado.Mensaje);
    }
}
