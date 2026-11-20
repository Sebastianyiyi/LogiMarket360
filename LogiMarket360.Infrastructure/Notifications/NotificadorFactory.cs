namespace LogiMarket360.Infrastructure.Notifications;

using LogiMarket360.Application.Ports;

// Factory Method: Creador abstracto de notificadores
public abstract class NotificadorCreator
{
    public abstract INotificador CrearNotificador();

    public async Task<bool> NotificarAsync(string destinatario, string mensaje)
    {
        var notificador = CrearNotificador();
        return await notificador.EnviarAsync(destinatario, mensaje);
    }
}

// Producto EMAIL
public class NotificadorEmail : INotificador
{
    private static readonly List<string> _bitacora = new();
    public static IReadOnlyList<string> Bitacora => _bitacora.AsReadOnly();

    public string Canal => "EMAIL";

    public Task<bool> EnviarAsync(string destinatario, string mensaje, CancellationToken ct = default)
    {
        string registro = $"[EMAIL] Para: {destinatario} | {DateTime.UtcNow:HH:mm:ss} | {mensaje}";
        _bitacora.Add(registro);
        Console.WriteLine(registro);
        return Task.FromResult(true);
    }
}

// Producto SMS
public class NotificadorSms : INotificador
{
    private static readonly List<string> _bitacora = new();
    public static IReadOnlyList<string> Bitacora => _bitacora.AsReadOnly();

    public string Canal => "SMS";

    public Task<bool> EnviarAsync(string destinatario, string mensaje, CancellationToken ct = default)
    {
        string textoCorto = mensaje.Length > 160 ? mensaje[..157] + "..." : mensaje;
        string registro = $"[SMS] Para: {destinatario} | {DateTime.UtcNow:HH:mm:ss} | {textoCorto}";
        _bitacora.Add(registro);
        Console.WriteLine(registro);
        return Task.FromResult(true);
    }
}

// Creador concreto EMAIL (Factory Method)
public class NotificadorEmailCreator : NotificadorCreator
{
    public override INotificador CrearNotificador() => new NotificadorEmail();
}

// Creador concreto SMS (Factory Method)
public class NotificadorSmsCreator : NotificadorCreator
{
    public override INotificador CrearNotificador() => new NotificadorSms();
}
