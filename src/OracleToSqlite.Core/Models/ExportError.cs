namespace OracleToSqlite.Core.Models;

public static class ExportErrorCodes
{
    public const string OracleConnectionFailed = "ORACLE_CONNECTION_FAILED";
    public const string OracleAuthenticationFailed = "ORACLE_AUTH_FAILED";
    public const string OracleSqlFailed = "ORACLE_SQL_FAILED";
    public const string OracleUnknownError = "ORACLE_UNKNOWN_ERROR";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string SqliteExportFailed = "SQLITE_EXPORT_FAILED";
    public const string ExportCancelled = "EXPORT_CANCELLED";
}

public sealed record ExportError(string Code, string Message, string? Detail = null);
