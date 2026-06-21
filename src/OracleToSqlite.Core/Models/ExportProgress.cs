namespace OracleToSqlite.Core.Models;

public sealed record ExportProgress(
    ExportStatus Status,
    long RowsWritten,
    string Message);
