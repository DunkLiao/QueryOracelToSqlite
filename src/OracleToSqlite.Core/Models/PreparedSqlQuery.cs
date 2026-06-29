namespace OracleToSqlite.Core.Models;

public sealed record PreparedSqlQuery(
    string SqlQuery,
    IReadOnlyList<QueryParameter> Parameters);
