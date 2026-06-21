using OracleToSqlite.Core.Models;

namespace OracleToSqlite.Core.Services;

public interface IOracleQueryService
{
    Task TestConnectionAsync(
        OracleConnectionSettings settings,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OracleColumnSchema>> GetSchemaAsync(
        OracleConnectionSettings settings,
        string sqlQuery,
        CancellationToken cancellationToken = default);

    Task<OracleQueryResult> ExecuteQueryAsync(
        OracleConnectionSettings settings,
        string sqlQuery,
        CancellationToken cancellationToken = default);
}
