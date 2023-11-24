using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Data.SqlClient;

namespace Lab1.DataLayer;

public static class QueryBuilderHelper
{
    public const string CurrentIndexParameterName = "currentIndex";
    public const string ValueVariablePrefix = "v";
    public const string TempTableName = "tempTable1";

    private static IEnumerable<T> AppendFront<T>(
        this IEnumerable<T> t,
        T value)
    {
        yield return value;
        foreach (var v in t)
            yield return v;
    }

    public static void BuildGetRowAtIndexQuery(
        StringBuilder sb,
        TableModel tableSchema)
    {
        var idPropertiesList = tableSchema.IdColumns
            .Select(c => c.Name)
            .JoinableDbPropertyList();
        var allPropertiesList = tableSchema.Columns
            .Select(c => c.Name)
            .AppendFront("rowNumberX")
            .JoinableDbPropertyList();

        using var t1Props = allPropertiesList.Prefix("t1");
        using var tProps = idPropertiesList.Prefix("t");
        using var t2Props = idPropertiesList.Prefix("t2");

        sb.Append($"""
        SELECT TOP(3) FROM
        (
            SELECT {t1Props}
            (
                SELECT
                    t.*,
                    ROW_NUMBER() OVER (
                        PARTITION BY {tProps}
                        ORDER BY {tProps}) AS rowNumberX
                FROM {tableSchema.FullyQualifiedName} AS t
            ) AS t1
            WHERE t1.rowNumberX <= @{CurrentIndexParameterName} + 1
                AND t1.rowNumberX >= @{CurrentIndexParameterName} - 1
        ) AS t2
        ORDER BY {t2Props}
        """);
    }

    public static IEnumerable<SqlParameter> GetParametersForGetRowAtIndexQuery(int currentIndex)
    {
        var currentIndexParameter = new SqlParameter(
            CurrentIndexParameterName,
            SqlDbType.Int)
        {
            Value = currentIndex,
        };
        yield return currentIndexParameter;
    }

    private static Dictionary<Type, SqlDbType> _TypeMap = new()
    {
        { typeof(byte), SqlDbType.TinyInt },
        { typeof(sbyte), SqlDbType.SmallInt },
        { typeof(short), SqlDbType.SmallInt },
        { typeof(ushort), SqlDbType.Int },
        { typeof(int), SqlDbType.Int },
        { typeof(uint), SqlDbType.BigInt },
        { typeof(long), SqlDbType.BigInt },
        { typeof(ulong), SqlDbType.Decimal },
        { typeof(float), SqlDbType.Real },
        { typeof(double), SqlDbType.Float },
        { typeof(decimal), SqlDbType.Money },
        { typeof(bool), SqlDbType.Bit },
        { typeof(string), SqlDbType.NVarChar },
        { typeof(char), SqlDbType.NChar },
        { typeof(Guid), SqlDbType.UniqueIdentifier },
        { typeof(DateTime), SqlDbType.DateTime2 },
        { typeof(DateTimeOffset), SqlDbType.DateTimeOffset },
        { typeof(byte[]), SqlDbType.VarBinary },
    };

    private static SqlParameter CreateParameterForType(Type type)
    {
        bool isNullable = false;
        if (Nullable.GetUnderlyingType(type) is { } underlyingType)
        {
            type = underlyingType;
            isNullable = true;
        }

        if (!_TypeMap.TryGetValue(type, out var dbSqlType))
        {
            throw new ArgumentException($"Unsupported type: {type.FullName}");
        }

        var result = new SqlParameter
        {
            SqlDbType = dbSqlType,
            IsNullable = isNullable,
        };
        return result;
    }

    private static IEnumerable<SqlParameter> SetValues(
        IEnumerable<int> columnIndices,
        List<string> allValues,
        TableModel tableSchema)
    {
        foreach (var i in columnIndices)
        {
            var columnSchema = tableSchema.Columns[i];
            var parameter = CreateParameterForType(columnSchema.Type);
            parameter.Value = Convert.ChangeType(allValues[i], columnSchema.Type);
            yield return parameter;
        }
    }

    public readonly struct VariableName : ISpanFormattable
    {
        private readonly int _index;
        public VariableName(int index) => _index = index;

        public string ToString(
            string? format,
            IFormatProvider? formatProvider)
        {
            throw new NotImplementedException();
        }

        public bool TryFormat(
            Span<char> destination,
            out int charsWritten,
            ReadOnlySpan<char> format,
            IFormatProvider? provider)
        {
            return destination.TryWrite(
                provider,
                $"@{ValueVariablePrefix}{_index}",
                out charsWritten);
        }
    }

    public static void BuildDeleteRowWithKeyQuery(
        StringBuilder sb,
        TableModel tableSchema)
    {
        var equalValueList = tableSchema.IdColumns
            .Select(x => new FullyQualifiedName("t", x.Name))
            .EqualValueList(i => new VariableName(i), delimiter: " AND ");
        sb.Append($"""
        DELETE FROM {tableSchema.FullyQualifiedName} AS t
        WHERE {equalValueList}
        """);
    }

    private static IEnumerable<int> SelectIndices(
        this IEnumerable<ColumnSchema> columns,
        Func<ColumnSchema, bool> predicate)
    {
        return columns.Select((c, i) => (c, i)).Where(x => predicate(x.c)).Select(x => x.i);
    }

    public static IEnumerable<SqlParameter> GetParametersForDeleteRowWithKeyQuery(
        TableModel tableSchema,
        List<string> allValues)
    {
        return SetValues(
            tableSchema.Columns.SelectIndices(c => c.IsId),
            allValues,
            tableSchema);
    }

    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public static void BuildInsertRowWithValuesQuery(
        StringBuilder sb,
        TableModel tableSchema)
    {
        var idColumns = tableSchema.IdColumns.ToArray();
        var keyPropertiesList = idColumns
            .Select(c => c.Name)
            .JoinableDbPropertyList();
        var tempKeyProps = keyPropertiesList.Prefix("t");

        // A hack to create an empty temporary table.
        // We delete before because one of the previous batches might have failed,
        // but since the table is per connection, it will stay.
        sb.AppendLine($"""
        IF OBJECT_ID('tempdb..#{TempTableName}') IS NOT NULL
            DROP TABLE #{TempTableName};
            
        SELECT ({tempKeyProps})
        INTO #{TempTableName}
        FROM {tableSchema.FullyQualifiedName} AS t
        WHERE 1=0;
        """);

        var notAutogeneratedColumns = tableSchema.Columns.Where(c => !c.IsAutoGenerated);
        var propertiesList = notAutogeneratedColumns.Select(c => c.Name)
            .JoinableDbPropertyList()
            .Prefix(null);
        var valuesList = notAutogeneratedColumns
            .Select((_, i) => new VariableName(i))
            .JoinableDbPropertyList()
            .Prefix(null);
        var insertedKeyProps = keyPropertiesList.Prefix("INSERTED");

        sb.AppendLine(/*lang=sql*/$"""
        INSERT INTO {tableSchema.FullyQualifiedName} AS t
        (
            {propertiesList}
        )
        OUTPUT {keyPropertiesList} INTO #{TempTableName}({insertedKeyProps})
        VALUES
        (
            {valuesList}
        );
        """);

        // ReSharper disable once InconsistentNaming
        var t1t2KeyEqualList = idColumns
            .Select(x => new FullyQualifiedName("t1", x.Name))
            .EqualValueList(i => new FullyQualifiedName("t2", idColumns[i].Name), delimiter: " AND ");

        // Now find the row index of the inserted row.
        sb.AppendLine($"""
        SELECT TOP(1) t1.rowNumberX FROM
        (
            SELECT
                t.*,
                ROW_NUMBER() OVER (
                    PARTITION BY {tempKeyProps} 
                    ORDER BY {tempKeyProps}) AS rowNumberX
            FROM #{TempTableName} AS t
            ORDER BY {tempKeyProps}
        ) AS t1
        WHERE EXISTS (
            SELECT 1 FROM {tableSchema.FullyQualifiedName} AS t2
            WHERE {t1t2KeyEqualList}
        );
        
        DROP TABLE #{TempTableName};
        """);
    }

    public static IEnumerable<SqlParameter> GetParametersForInsertRowWithValuesQuery(
        TableModel tableSchema,
        List<string> allValues)
    {
        return SetValues(
            tableSchema.Columns.SelectIndices(c => !c.IsAutoGenerated),
            allValues,
            tableSchema);
    }

    public static void BuildUpdateRowQuery(
        StringBuilder sb,
        TableModel tableSchema)
    {
        var notIdColumns = tableSchema.Columns.Where(c => !c.IsId);
        int nonIdColumnCount = notIdColumns.Count();
        var notIdColumnAssignments = notIdColumns
            .Select(c => new FullyQualifiedName("t", c.Name))
            .EqualValueList(i => new VariableName(i), delimiter: ", ");
        var keyColumnChecks = tableSchema.Columns.Where(c => c.IsId)
            .Select(x => new FullyQualifiedName("t", x.Name))
            .EqualValueList(i => new VariableName(i + nonIdColumnCount), delimiter: " AND ");

        sb.Append($"""
        UPDATE {tableSchema.FullyQualifiedName} AS t
        SET {notIdColumnAssignments}
        WHERE {keyColumnChecks}
        """);
    }

    public static IEnumerable<SqlParameter> GetParametersForUpdateRowQuery(
        TableModel tableSchema,
        List<string> allValues)
    {
        var notIdColumnIndices = tableSchema.Columns.SelectIndices(c => !c.IsId);
        var idColumnIndices = tableSchema.Columns.SelectIndices(c => c.IsId);

        var a = SetValues(
            notIdColumnIndices,
            allValues,
            tableSchema);

        var b = SetValues(
            idColumnIndices,
            allValues,
            tableSchema);

        return a.Concat(b);
    }


    public static void AddRange(
        this SqlParameterCollection collection,
        IEnumerable<SqlParameter> parameters)
    {
        foreach (var parameter in parameters)
        {
            collection.Add(parameter);
        }
    }

}