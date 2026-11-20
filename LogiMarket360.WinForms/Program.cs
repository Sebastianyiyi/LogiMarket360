namespace LogiMarket360.WinForms;

using LogiMarket360.Infrastructure.Data;
using LogiMarket360.Infrastructure.DI;
using Microsoft.Extensions.DependencyInjection;
using LogiMarket360.WinForms.Views;

internal static class Program
{
    [STAThread]
    private static async Task Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            string dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LogiMarket360");
            Directory.CreateDirectory(dataDirectory);
            string databasePath = Path.Combine(dataDirectory, "logimarket360.db");

            var services = new ServiceCollection();
            services.AddInfrastructure($"Data Source={databasePath}");
            services.AddScoped<LogiMarketView>();
            services.AddScoped<CatalogoControl>();
            services.AddScoped<ClientesControl>();
            services.AddScoped<PedidosControl>();
            services.AddScoped<LogisticaControl>();
            services.AddScoped<DevolucionesControl>();
            services.AddScoped<ConsultasControl>();

            using var provider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<LogiMarketDbContext>();
            await DatabaseSeeder.InicializarAsync(db);

            var mainForm = scope.ServiceProvider.GetRequiredService<LogiMarketView>();
            global::System.Windows.Forms.Application.Run(mainForm);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No fue posible iniciar LogiMarket 360.\n\n{ex.Message}",
                "Error de inicio",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
