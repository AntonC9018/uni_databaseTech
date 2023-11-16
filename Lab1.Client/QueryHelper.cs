using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Printing;
using System.Text;
using Laborator1;
using Microsoft.Data.SqlClient;

namespace Lab1.DataLayer;

public static class QueryBuilderHelper
{
    public const string CurrentIndexParameterName = "currentIndex";
    public const string ValueVariablePrefix = "v";

    public static void BuildGetRowAtIndexQuery(
        StringBuilder sb,
        TableSchemaViewModel tableSchema)
    {
        var propertiesList = tableSchema.Columns
            .Select(c => c.Name)
            .JoinableDbPropertyList();
        var idPropertiesList = tableSchema.IdColumns
            .Select(c => c.Name)
            .JoinableDbPropertyList();

        using var t1Props = propertiesList.Prefix("t1");
        using var tProps = idPropertiesList.Prefix("t");
        using var t2Props = idPropertiesList.Prefix("t2");

        sb.Append($"""
        SELECT TOP(1) FROM
        (
            SELECT {t1Props} FROM
            (
                SELECT
                    t.*,
                    ROW_NUMBER() OVER (
                        PARTITION BY {tProps}) AS rowNumberX
                FROM {tableSchema.FullyQualifiedName} AS t
            ) AS t1
            WHERE t1.rowNumberX <= @{CurrentIndexParameterName}
        ) AS t2
        ORDER BY {t2Props}
        """);
    }

    public static SqlParameter[] GetParametersForGetRowAtIndexQuery(int currentIndex)
    {
        var currentIndexParameter = new SqlParameter(
            CurrentIndexParameterName,
            System.Data.SqlDbType.Int)
        {
            Value = currentIndex
        };
        return new[]
        { 
            currentIndexParameter
        };
    }

    private static Dictionary<Type, SqlDbType> _TypeMap = new Dictionary<Type, SqlDbType>
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
        if (Nullable.GetUnderlyingType(type) is Type underlyingType)
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

    private static void SetValues(
        IEnumerable<int> columnIndices,
        Span<SqlParameter> outParameters,
        object[] allValues,
        TableSchemaViewModel tableSchema)
    {
        int paramIndex = 0;
        foreach (var i in columnIndices)
        {
            var columnSchema = tableSchema.Columns[i];
            var parameter = CreateParameterForType(columnSchema.Type);
            outParameters[paramIndex] = parameter;
            paramIndex++;
            parameter.Value = Convert.ChangeType(allValues[i], columnSchema.Type);
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
        TableSchemaViewModel tableSchema)
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

    public static SqlParameter[] GetParametersForDeleteRowWithKeyQuery(
        TableSchemaViewModel tableSchema,
        string[] columnValues)
    {
        var ids = tableSchema.Columns.Where(c => c.IsId);
        var keyColumnCount = ids.Count();
        var result = new SqlParameter[keyColumnCount];

        SetValues(
            tableSchema.Columns.SelectIndices(c => c.IsId),
            result,
            columnValues,
            tableSchema);
        return result;
    }

    public static void BuildInsertRowWithValuesQuery(
        StringBuilder sb,
        TableSchemaViewModel tableSchema)
    {
        var notAutogeneratedColumns = tableSchema.Columns.Where(c => !c.IsAutoGenerated);
        var propertiesList = notAutogeneratedColumns.Select(c => c.Name)
            .JoinableDbPropertyList()
            .Prefix(null);
        var valuesList = notAutogeneratedColumns
            .Select((_, i) => new VariableName(i))
            .JoinableDbPropertyList()
            .Prefix(null);

        sb.Append($"""
        INSERT INTO {tableSchema.FullyQualifiedName} AS t
        (
            {notAutogeneratedColumns}
        )
        VALUES
        (
            {valuesList}
        )
        """);
    }

    public static SqlParameter[] GetParametersForInsertRowWithValuesQuery(
        TableSchemaViewModel tableSchema,
        string[] columnValues)
    {
        var notAutogeneratedColumns = tableSchema.Columns.Where(c => !c.IsAutoGenerated);
        var notAutogeneratedColumnCount = notAutogeneratedColumns.Count();
        var result = new SqlParameter[notAutogeneratedColumnCount];

        SetValues(
            tableSchema.Columns.SelectIndices(c => !c.IsAutoGenerated),
            result,
            columnValues,
            tableSchema);
        return result;
    }

    public static void BuildUpdateRowQuery(
        StringBuilder sb,
        TableSchemaViewModel tableSchema)
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

    public static SqlParameter[] GetParametersForUpdateRowQuery(
        TableSchemaViewModel tableSchema,
        string[] columnValues)
    {
        var notIdColumnIndices = tableSchema.Columns.SelectIndices(c => !c.IsId);
        var idColumnIndices = tableSchema.Columns.SelectIndices(c => c.IsId);
        var result = new SqlParameter[tableSchema.Columns.Length];
        var notIdColumnIndicesCount = notIdColumnIndices.Count();

        SetValues(
            notIdColumnIndices,
            result.AsSpan()[0 .. notIdColumnIndicesCount],
            columnValues,
            tableSchema);
        
        SetValues(
            idColumnIndices,
            result.AsSpan()[notIdColumnIndicesCount ..],
            columnValues,
            tableSchema);

        return result;
    }

}