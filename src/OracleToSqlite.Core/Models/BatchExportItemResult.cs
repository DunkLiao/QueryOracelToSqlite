namespace OracleToSqlite.Core.Models;

public sealed record BatchExportItemResult(
    string SqlFilePath,
    string TargetTableName,
    ExportStatus Status,
    long RowCount,
    ExportError? Error);
