namespace LogiMarket360.Domain.Exceptions;

public class CicloDetectadoException : DomainException
{
    public CicloDetectadoException(string codigoKit, string mensaje) 
        : base($"Referencia circular detectada en el kit '{codigoKit}': {mensaje}")
    {
    }
}
