namespace OracleToSqlite.Core.Models;

public sealed record ExportResult(
    ExportStatus Status,
    long RowCount,
    TimeSpan Elapsed,
    string? OutputPath,
    ExportError? Error)
{
    public bool Success => Status == ExportStatus.Succeeded;

    public static ExportResult Succeeded(long rowCount, TimeSpan elapsed, string outputPath)
    {
        return new ExportResult(ExportStatus.Succeeded, rowCount, elapsed, outputPath, null);
    }

    public static ExportResult Failed(TimeSpan elapsed, ExportError error)
    {
        return new ExportResult(ExportStatus.Failed, 0, elapsed, null, error);
    }
}
