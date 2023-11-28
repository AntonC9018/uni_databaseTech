using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace Lab1.DataLayer;

public sealed class DeleteRowWithKeyQueryBuilder : IQueryBuilder
{
    public void Build(StringBuilder sb, TableModel tableSchema)
    {
        QueryBuilderHelper.BuildDeleteRowWithKeyQuery(sb, tableSchema);
    }

    public IEnumerable<SqlParameter> GetParameters(TableModel tableSchema)
    {
        return QueryBuilderHelper.GetParametersForDeleteRowWithKeyQuery(tableSchema);
    }
}

public sealed class InsertRowWithValuesQueryBuilder : IQueryBuilder
{
    public void Build(StringBuilder sb, TableModel tableSchema)
    {
        QueryBuilderHelper.BuildInsertRowWithValuesQuery(sb, tableSchema);
    }

    public IEnumerable<SqlParameter> GetParameters(TableModel tableSchema)
    {
        return QueryBuilderHelper.GetParametersForInsertRowWithValuesQuery(tableSchema);
    }
}

public sealed class UpdateRowQueryBuilder : IQueryBuilder
{
    public void Build(StringBuilder sb, TableModel tableSchema)
    {
        QueryBuilderHelper.BuildUpdateRowQuery(sb, tableSchema);
    }

    public IEnumerable<SqlParameter> GetParameters(TableModel tableSchema)
    {
        return QueryBuilderHelper.GetParametersForUpdateRowQuery(tableSchema);
    }
}