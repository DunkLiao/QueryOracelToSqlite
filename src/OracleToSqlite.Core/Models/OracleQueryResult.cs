namespace OracleToSqlite.Core.Models;

public sealed record OracleQueryResult(
    IReadOnlyList<OracleColumnSchema> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows);
