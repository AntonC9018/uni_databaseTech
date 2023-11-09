using System.Linq;
using System.Text;
using Laborator1;

namespace Lab1.DataLayer;

public static class QueryBuilderHelper
{
    public const string CurrentIndexParameterName = "currentIndex";

    public static void GetRowAtIndexQuery(
        StringBuilder sb,
        TableSchemaViewModel tableSchema)
    {
        var propertiesList = new JoinableDbPropertyList(tableSchema.Columns.Select(c => c.Name));
        var idPropertiesList = new JoinableDbPropertyList(tableSchema.IdColumns.Select(c => c.Name));
        sb.Append($"""
        SELECT TOP(1) FROM
        (
            SELECT {propertiesList.Prefix("t1")} FROM
            (
                SELECT
                    t.*,
                    ROW_NUMBER() OVER (
                        PARTITION BY {idPropertiesList.Prefix("t")}) AS rowNumberX
                FROM {tableSchema.Schema}.[{tableSchema.Name}] AS t
            ) AS t1
            WHERE t1.rowNumberX <= @{CurrentIndexParameterName}
        ) AS t2
        ORDER BY {idPropertiesList.Prefix("t2")}
        """);
    }
}