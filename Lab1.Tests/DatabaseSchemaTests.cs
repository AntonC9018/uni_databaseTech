using Lab1.DataLayer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace TestProject1;

[UsesVerify]
public class DatabaseSchemaTests
{
    [Fact]
    public async Task Test1()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
        var connectionString = config.GetValue<string>("ConnectionStrings:MainDatabase");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        var tables = await DatabaseSchemaHelper.GetTables(connection);
        await Verify(tables);
    }


    [Fact]
    public void JoinableDbPropertyListTest()
    {
        var list = new[] { "a", "b", "c" }.JoinableDbPropertyList();
        var result = list.Prefix("t");
        var str = $"{result}";
        Assert.Equal("t.[a], t.[b], t.[c]", str);
    }

    [Fact]
    public void JoinableDbPropertyListTestWithNull()
    {
        var list = new[] { "a", "b", "c" }.JoinableDbPropertyList();
        var result = list.Prefix(null);
        var str = $"{result}";
        Assert.Equal("[a], [b], [c]", str);
    }

    [Fact]
    public Task JoinableDbPropertyList_ReusedEnumerable()
    {
        IEnumerable<string> Get()
        {
            for (int i = 0; i < 100; i++)
                yield return "abcdefg" + i;
        }

        var e = Get();
        var list = e.JoinableDbPropertyList();

        var tList = list.Prefix("t");
        var t1List = list.Prefix("t1");
        var result1 = $"{tList}";
        var result2 = $"{t1List}";

        return Verify(new
        {
            result1,
            result2
        });
    }
}