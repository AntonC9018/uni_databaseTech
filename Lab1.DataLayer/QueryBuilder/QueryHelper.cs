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
        using var tPropsA = idPropertiesList.Prefix("t");
        using var tPropsB = idPropertiesList.Prefix("t");
        using var t2Props = idPropertiesList.Prefix("t2");

        sb.Append($"""
        SELECT * FROM
        (
            SELECT {t1Props} FROM
            (
                SELECT
                    t.*,
                    ROW_NUMBER() OVER (ORDER BY {tPropsB}) - 1 AS rowNumberX
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


    private sealed class VariableNamer
    {
        private int _counter = 0;

        public string Next()
        {
            var result = new VariableName(_counter);
            _counter++;
            return result.ToString();
        }
    }

    private static IEnumerable<SqlParameter> SetValues(
        IEnumerable<int> columnIndices,
        List<string> allValues,
        TableModel tableSchema,
        VariableNamer namer)
    {
        foreach (var i in columnIndices)
        {
            var columnSchema = tableSchema.Columns[i];
            var parameter = CreateParameterForType(columnSchema.Type);
            parameter.ParameterName = namer.Next();
            parameter.Value = Convert.ChangeType(allValues[i], columnSchema.Type);
            yield return parameter;
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
            tableSchema,
            new());
    }

    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public static void BuildInsertRowWithValuesQuery(
        StringBuilder sb,
        TableModel tableSchema)
    {
        sb.AppendLine($"SET IDENTITY_INSERT {tableSchema.FullyQualifiedName} OFF;");

        var idColumns = tableSchema.IdColumns.ToArray();
        var keyPropertiesListGenerator = idColumns
            .Select(c => c.Name)
            .JoinableDbPropertyList();

        {
            // TODO: Make this in a non-lazy way.
            using var keyPropNameList = idColumns
                .Select(c => $"[{c.Name}] {c.DatabaseType}")
                .JoinableDbPropertyList(needsSquareBrackets: false)
                .Prefix(null);

            sb.AppendLine($"""
            IF OBJECT_ID('tempdb..#{TempTableName}') IS NOT NULL
                DROP TABLE #{TempTableName};
                
            CREATE TABLE #{TempTableName}({keyPropNameList});
            """);
        }

        var notAutogeneratedColumns = tableSchema.Columns.Where(c => !c.IsAutoGenerated);
        {
            using var propertiesList = notAutogeneratedColumns
                .Select(c => c.Name)
                .JoinableDbPropertyList()
                .Prefix(null);
            using var valuesList = notAutogeneratedColumns
                .Select((_, i) => new VariableName(i))
                .JoinableDbPropertyList()
                .Prefix(null);
            using var insertedKeyProps = keyPropertiesListGenerator.Prefix("INSERTED");
            using var tempKeyPropsWithoutPrefix = keyPropertiesListGenerator.Prefix(null);

            sb.AppendLine(/*lang=sql*/$"""
            INSERT INTO {tableSchema.FullyQualifiedName}
            (
                {propertiesList}
            )
            OUTPUT {insertedKeyProps}
                INTO #{TempTableName}({tempKeyPropsWithoutPrefix})
            VALUES
            (
                {valuesList}
            );
            """);
        }

        {
            using var tempKeyPropsB = keyPropertiesListGenerator.Prefix("t");

            // ReSharper disable once InconsistentNaming
            using var t1t2KeyEqualList = idColumns
                .Select(x => new FullyQualifiedName("t1", x.Name))
                .EqualValueList(i => new FullyQualifiedName("t2", idColumns[i].Name), delimiter: " AND ");

            // Now find the row index of the inserted row.
            sb.AppendLine($"""
            SELECT TOP(1) t1.rowNumberX FROM
            (
                SELECT
                    t.*,
                    ROW_NUMBER() OVER (
                        ORDER BY {tempKeyPropsB}) - 1 AS rowNumberX
                FROM #{TempTableName} AS t
            ) AS t1
            WHERE EXISTS (
                SELECT 1 FROM {tableSchema.FullyQualifiedName} AS t2
                WHERE {t1t2KeyEqualList}
            );
            
            DROP TABLE #{TempTableName};
            """);
        }
    }

    public static IEnumerable<SqlParameter> GetParametersForInsertRowWithValuesQuery(
        TableModel tableSchema,
        List<string> allValues)
    {
        return SetValues(
            tableSchema.Columns.SelectIndices(c => !c.IsAutoGenerated),
            allValues,
            tableSchema,
            new());
    }

    public static void BuildUpdateRowQuery(
        StringBuilder sb,
        TableModel tableSchema)
    {
        var notIdColumns = tableSchema.Columns
            .Where(c => !c.IsId);
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
        var namer = new VariableNamer();

        var a = SetValues(
            notIdColumnIndices,
            allValues,
            tableSchema,
            namer);

        var b = SetValues(
            idColumnIndices,
            allValues,
            tableSchema,
            namer);

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

    public static bool IsTypeRepresentableAsString(this ColumnSchema column)
    {
        return column.Type != typeof(byte[]);
    }

    public static IEnumerable<ColumnSchema> WhereTypeRepresentableAsString(
        this IEnumerable<ColumnSchema> columns)
    {
        return columns.Where(IsTypeRepresentableAsString);
    }
}

public interface IRequiresSquareBrackets
{
    bool RequiresSquareBrackets { get; }
}

public readonly struct VariableName : ISpanFormattable, IRequiresSquareBrackets
{
    private readonly int _index;
    public VariableName(int index) => _index = index;

    public bool RequiresSquareBrackets => false;

    public string ToString(
        string? format = null,
        IFormatProvider? formatProvider = null)
    {
        return $"{this}";
    }

    public bool TryFormat(
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        return destination.TryWrite(
            provider,
            $"@{QueryBuilderHelper.ValueVariablePrefix}{_index}",
            out charsWritten);
    }
}