using lab4.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace lab4;

static class Program
{
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((_, builder) =>
            {
                builder.AddJsonFile("appsettings.json");
            })
            .ConfigureServices((context, services) =>
            {
                services.AddDbContext<MyDbContext>(options =>
                {
                    // Configure the connection string.
                    var configuration = context.Configuration;
                    var connectionString = configuration.GetConnectionString("MainDatabase");
                    options.UseSqlServer(connectionString);
                });
                services.AddScoped<DataGridForm>();
            });
    }

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var host = CreateHostBuilder(args).Build();
        var scope = host.Services.CreateScope();
        var form = scope.ServiceProvider.GetRequiredService<DataGridForm>();
        Application.ApplicationExit += (_, _) => scope.Dispose();
        Application.Run(form);
    }
}
