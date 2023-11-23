using System;
using System.Linq;
using System.Threading;
using System.Windows;
using Lab1.DataLayer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Laborator1;

public sealed partial class App : Application
{
    private void Main(object sender, StartupEventArgs e)
    {
        this.DispatcherUnhandledException += static (_, e) =>
        {
            MessageBox.Show(
                "An unhandled exception just occurred: " + e.Exception.Message,
                e.Exception.GetType().FullName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        };

        var applicationCts = new CancellationTokenSource();
        this.Exit += (_, _) => applicationCts.Cancel();

        var builder = Host.CreateDefaultBuilder();
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddJsonFile("appsettings.json", optional: false);
        });

        builder.ConfigureServices((context, services) =>
        {
            services.AddScoped<SqlConnection>(_ =>
            {
                var connectionString = context.Configuration
                    .GetValue<string>("ConnectionStrings:MainDatabase");
                var connection = new SqlConnection(connectionString);
                return connection;
            });

            services.AddScoped<MainMenuViewModel>(sp =>
            {
                var connection = sp.GetRequiredService<SqlConnection>();
                return new MainMenuViewModel(
                    connection,
                    applicationCts.Token,
                    new());
            });
            services.AddScoped<MainMenu>();
        });

        var app = builder.Build();

        var scope = app.Services.CreateScope();
        var mainMenu = scope.ServiceProvider.GetRequiredService<MainMenu>();
        mainMenu.Show();

        this.Exit += (_, _) => scope.Dispose();
    }
}

