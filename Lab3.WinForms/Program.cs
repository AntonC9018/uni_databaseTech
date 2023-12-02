using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace lab3;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false, true)
            .Build();
        var connectionString = configuration.GetConnectionString("MainDatabase");
        var connection = new SqlConnection(connectionString);
        connection.Open();
        var form = new DataGridForm(connection);
        Application.ApplicationExit += (_, _) => connection.Dispose();
        Application.Run(form);
    }
}