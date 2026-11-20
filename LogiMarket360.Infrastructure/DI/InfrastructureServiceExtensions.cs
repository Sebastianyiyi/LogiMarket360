namespace LogiMarket360.Infrastructure.DI;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LogiMarket360.Application.Facade;
using LogiMarket360.Application.Patterns.Decorator;
using LogiMarket360.Application.Patterns.Flyweight;
using LogiMarket360.Application.Patterns.Observer;
using LogiMarket360.Application.Patterns.Observer.Eventos;
using LogiMarket360.Application.Patterns.Observer.Subscriptores;
using LogiMarket360.Application.Patterns.Proxy;
using LogiMarket360.Application.Patterns.Singleton;
using LogiMarket360.Application.Patterns.TemplateMethod;
using LogiMarket360.Application.Ports;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Application.Security;
using LogiMarket360.Application.Services;
using LogiMarket360.Domain.Enums;
using LogiMarket360.Infrastructure.Adapters;
using LogiMarket360.Infrastructure.Data;
using LogiMarket360.Infrastructure.Delivery;
using LogiMarket360.Infrastructure.Documents;
using LogiMarket360.Infrastructure.Notifications;
using LogiMarket360.Infrastructure.Repositories;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        Func<RolUsuario?>? obtenerRolActual = null,
        bool usarPasarelaFallo = false,
        string canalNotificacion = "EMAIL")
    {
        services.AddDbContext<LogiMarketDbContext>(options =>
            options.UseSqlite(connectionString));

        // Sesión y autorización
        services.AddSingleton<SesionServicio>();
        services.AddSingleton<ISesionActual>(sp => sp.GetRequiredService<SesionServicio>());
        services.AddSingleton<IAutorizador>(sp => sp.GetRequiredService<SesionServicio>());

        // Métricas de sesión (Singleton compartido)
        services.AddSingleton<MetricasSesion>(MetricasSesion.Instancia);

        // Unit of Work y repositorios
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddScoped<IKitRepository, KitRepository>();
        services.AddScoped<IPedidoRepository, PedidoRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IMovimientoInventarioRepository, MovimientoInventarioRepository>();
        services.AddScoped<IDevolucionRepository, DevolucionRepository>();
        services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
        services.AddScoped<IEventoOutboxRepository, EventoOutboxRepository>();
        services.AddScoped<IPagoRepository, PagoRepository>();
        services.AddScoped<IDespachoRepository, DespachoRepository>();

        // Pasarela: Adapter legado → Decorator de auditoría → Proxy de autorización
        services.AddSingleton<PasarelaLegadaSimulada>();
        services.AddSingleton<IEntregaFactoryProvider, EntregaFactoryProvider>();
        services.AddSingleton<INotificadorFactory>(_ => new NotificadorFactory(canalNotificacion));
        services.AddSingleton<ISalidaDocumentoFactory>(_ => new SalidaDocumentoFactory());
        services.AddSingleton<CatalogoVisualFlyweightFactory>();

        services.AddTransient<IPasarelaPago>(sp =>
        {
            IPasarelaPago pasarelaBase = usarPasarelaFallo
                ? new PasarelaFallaSimulada()
                : new PasarelaLegadaAdapter(sp.GetRequiredService<PasarelaLegadaSimulada>());

            var decorator = new AuditoriaPagoDecorator(pasarelaBase, msg => Console.WriteLine(msg));
            Func<RolUsuario?> rol = obtenerRolActual ?? (() => sp.GetRequiredService<ISesionActual>().Rol);
            return new PagoAutorizadoProxy(decorator, rol);
        });

        // Observer / Publicador (scoped para inyectar repositorios scoped)
        services.AddScoped<IEventoPublicador>(sp =>
        {
            var publicador = new EventoPublicador();
            var metricas = sp.GetRequiredService<MetricasSesion>();
            var auditoriaRepository = sp.GetRequiredService<IAuditoriaRepository>();

            publicador.Suscribir<PedidoPagadoEvent>(new MetricasObserver(metricas));
            publicador.Suscribir<PedidoPagadoEvent>(new AuditoriaObserver(auditoriaRepository));
            publicador.Suscribir<DespachoRegistradoEvent>(new AuditoriaObserver(auditoriaRepository));
            publicador.Suscribir<DevolucionAceptadaEvent>(new MetricasObserver(metricas));
            publicador.Suscribir<DevolucionAceptadaEvent>(new AuditoriaObserver(auditoriaRepository));

            return publicador;
        });

        // Servicios de aplicación
        services.AddScoped<OutboxProcessor>();
        services.AddScoped<IOutboxProcesador>(sp => sp.GetRequiredService<OutboxProcessor>());
        services.AddScoped<AutenticacionServicio>();
        services.AddScoped<CatalogoServicio>();
        services.AddScoped<ClienteServicio>();
        services.AddScoped<PedidoBorradorServicio>();
        services.AddScoped<ComprobanteServicio>();
        services.AddScoped<ReporteServicio>();

        // Template Method de devoluciones
        services.AddScoped<DevolucionDefectoService>();
        services.AddScoped<DevolucionArrepentimientoService>();

        // Facade de confirmación
        services.AddScoped<ConfirmacionPedidoFacade>();

        return services;
    }
}
