using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var connectionString = config.GetValue<string>("ConnectionStrings:MainDatabase");
using var connection = new SqlConnection(connectionString);
connection.Open();
{
    using var command = connection.CreateCommand();
    command.CommandText = "SELECT * FROM Region";
    using var reader = command.ExecuteReader();
    foreach (var value in reader)
    {
        Console.WriteLine(value);
    }
}
connection.Close();
