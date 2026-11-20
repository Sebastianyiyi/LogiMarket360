namespace LogiMarket360.Infrastructure.DI;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LogiMarket360.Application.Facade;
using LogiMarket360.Application.Patterns.Observer;
using LogiMarket360.Application.Patterns.Observer.Subscriptores;
using LogiMarket360.Application.Patterns.Singleton;
using LogiMarket360.Application.Ports;
using LogiMarket360.Application.Ports.Repositories;
using LogiMarket360.Domain.Enums;
using LogiMarket360.Infrastructure.Adapters;
using LogiMarket360.Infrastructure.Data;
using LogiMarket360.Infrastructure.Delivery;
using LogiMarket360.Infrastructure.Flyweight;
using LogiMarket360.Infrastructure.Notifications;
using LogiMarket360.Infrastructure.Repositories;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        Func<RolUsuario>? obtenerRolActual = null,
        bool usarPasarelaFallo = false)
    {
        // EF Core + SQLite
        services.AddDbContext<LogiMarketDbContext>(options =>
            options.UseSqlite(connectionString));

        // Unit of Work
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        // Repositorios
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

        // Pasarela: Adapter legado → Decorator de Auditoría → Proxy de autorización
        services.AddSingleton<PasarelaLegadaSimulada>();

        if (usarPasarelaFallo)
        {
            // Para pruebas P06
            services.AddTransient<IPasarelaPago>(sp =>
            {
                var pasarelaFallo = new PasarelaFallaSimulada();
                return pasarelaFallo;
            });
        }
        else if (obtenerRolActual != null)
        {
            services.AddTransient<IPasarelaPago>(sp =>
            {
                var legada = sp.GetRequiredService<PasarelaLegadaSimulada>();
                var adapter = new PasarelaLegadaAdapter(legada);
                var decorator = new Application.Patterns.Decorator.AuditoriaPagoDecorator(
                    adapter, msg => Console.WriteLine(msg));
                return new Application.Patterns.Proxy.PagoAutorizadoProxy(decorator, obtenerRolActual);
            });
        }
        else
        {
            services.AddTransient<IPasarelaPago>(sp =>
            {
                var legada = sp.GetRequiredService<PasarelaLegadaSimulada>();
                return new PasarelaLegadaAdapter(legada);
            });
        }

        // Observer / Publicador
        services.AddSingleton<IEventoPublicador>(sp =>
        {
            var pub = new EventoPublicador();
            var metricas = sp.GetRequiredService<MetricasSesion>();
            var auditRepo = sp.GetRequiredService<IAuditoriaRepository>();

            pub.Suscribir<Application.Patterns.Observer.Eventos.PedidoPagadoEvent>(
                new MetricasObserver(metricas));
            pub.Suscribir<Application.Patterns.Observer.Eventos.PedidoPagadoEvent>(
                new AuditoriaObserver(auditRepo));
            pub.Suscribir<Application.Patterns.Observer.Eventos.DespachoRegistradoEvent>(
                new AuditoriaObserver(auditRepo));
            pub.Suscribir<Application.Patterns.Observer.Eventos.DevolucionAceptadaEvent>(
                new MetricasObserver(metricas));
            pub.Suscribir<Application.Patterns.Observer.Eventos.DevolucionAceptadaEvent>(
                new AuditoriaObserver(auditRepo));
            return pub;
        });

        // Singleton de métricas de sesión
        services.AddSingleton<MetricasSesion>(MetricasSesion.Instancia);

        // Flyweight factory
        services.AddSingleton<CatalogoVisualFlyweightFactory>();

        // Facade de confirmación
        services.AddScoped<ConfirmacionPedidoFacade>();

        return services;
    }
}
