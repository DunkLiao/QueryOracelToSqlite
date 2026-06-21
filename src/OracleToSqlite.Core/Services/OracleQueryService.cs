using System.Data;
using Oracle.ManagedDataAccess.Client;
using OracleToSqlite.Core.Models;

namespace OracleToSqlite.Core.Services;

public sealed class OracleQueryService : IOracleQueryService
{
    public async Task TestConnectionAsync(
        OracleConnectionSettings settings,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = CreateConnection(settings);
            await connection.OpenAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is OracleException or InvalidOperationException)
        {
            throw CreateQueryException(exception, "testing Oracle connection");
        }
    }

    public async Task<IReadOnlyList<OracleColumnSchema>> GetSchemaAsync(
        OracleConnectionSettings settings,
        string sqlQuery,
        CancellationToken cancellationToken = default)
    {
        ValidateSql(sqlQuery);

        try
        {
            await using var connection = CreateConnection(settings);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = sqlQuery;
            command.CommandType = CommandType.Text;

            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SchemaOnly,
                cancellationToken);

            return OracleColumnSchemaReader.FromSchemaTable(reader.GetSchemaTable());
        }
        catch (Exception exception) when (exception is OracleException or InvalidOperationException)
        {
            throw CreateQueryException(exception, "reading Oracle query schema");
        }
    }

    public async Task<OracleQueryResult> ExecuteQueryAsync(
        OracleConnectionSettings settings,
        string sqlQuery,
        CancellationToken cancellationToken = default)
    {
        ValidateSql(sqlQuery);

        try
        {
            await using var connection = CreateConnection(settings);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = sqlQuery;
            command.CommandType = CommandType.Text;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var columns = OracleColumnSchemaReader.FromSchemaTable(reader.GetSchemaTable());
            var rows = new List<IReadOnlyDictionary<string, object?>>();

            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                foreach (var column in columns)
                {
                    var value = reader.GetValue(column.Ordinal);
                    row[column.ColumnName] = value is DBNull ? null : value;
                }

                rows.Add(row);
            }

            return new OracleQueryResult(columns, rows);
        }
        catch (Exception exception) when (exception is OracleException or InvalidOperationException)
        {
            throw CreateQueryException(exception, "executing Oracle query");
        }
    }

    private static OracleConnection CreateConnection(OracleConnectionSettings settings)
    {
        return new OracleConnection(OracleConnectionStringFactory.Create(settings));
    }

    private static void ValidateSql(string sqlQuery)
    {
        if (string.IsNullOrWhiteSpace(sqlQuery))
        {
            throw new ArgumentException("SQL query is required.", nameof(sqlQuery));
        }
    }

    private static OracleQueryException CreateQueryException(Exception exception, string operation)
    {
        return new OracleQueryException(OracleErrorMapper.ToExportError(exception, operation), exception);
    }
}
