namespace OracleToSqlite.Core.Models;

public sealed record BatchExportResult(
    ExportStatus Status,
    int TotalFiles,
    int SucceededCount,
    int FailedCount,
    long TotalRows,
    TimeSpan Elapsed,
    string? OutputPath,
    IReadOnlyList<BatchExportItemResult> Items)
{
    public bool Success => Status == ExportStatus.Succeeded;
}
