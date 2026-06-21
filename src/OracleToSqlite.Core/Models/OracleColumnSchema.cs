namespace OracleToSqlite.Core.Models;

public sealed record OracleColumnSchema(
    int Ordinal,
    string ColumnName,
    string OracleDataTypeName,
    string ProviderTypeName,
    int? Precision,
    int? Scale,
    bool AllowDBNull);
