using System.Diagnostics;
using OracleToSqlite.Core.Models;

namespace OracleToSqlite.Core.Services;

public sealed class BatchExportJobRunner(IExportJobRunner exportJobRunner) : IBatchExportJobRunner
{
    public async Task<BatchExportResult> RunAsync(
        BatchExportJobSettings settings,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var items = new List<BatchExportItemResult>();

        try
        {
            ArgumentNullException.ThrowIfNull(settings);

            if (string.IsNullOrWhiteSpace(settings.SqlFolderPath) || !Directory.Exists(settings.SqlFolderPath))
            {
                return FailedValidation(
                    stopwatch,
                    items,
                    settings.SqlFolderPath ?? string.Empty,
                    "SQL folder path is required and must exist.");
            }

            if (string.IsNullOrWhiteSpace(settings.SqliteFilePath))
            {
                return FailedValidation(
                    stopwatch,
                    items,
                    settings.SqlFolderPath,
                    "SQLite output path is required.");
            }

            var sqlFiles = Directory
                .EnumerateFiles(settings.SqlFolderPath, "*.sql", SearchOption.TopDirectoryOnly)
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (sqlFiles.Length == 0)
            {
                return FailedValidation(
                    stopwatch,
                    items,
                    settings.SqlFolderPath,
                    "SQL folder must contain at least one .sql file.");
            }

            foreach (var sqlFile in sqlFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var targetTableName = Path.GetFileNameWithoutExtension(sqlFile);
                var completedRows = items
                    .Where(item => item.Status == ExportStatus.Succeeded)
                    .Sum(item => item.RowCount);
                progress?.Report(new ExportProgress(ExportStatus.Running, completedRows, $"Exporting {Path.GetFileName(sqlFile)}."));

                var sqlQuery = await File.ReadAllTextAsync(sqlFile, cancellationToken);
                var itemSettings = new ExportJobSettings
                {
                    Connection = settings.Connection,
                    SqlQuery = sqlQuery.Trim(),
                    SqliteFilePath = settings.SqliteFilePath.Trim(),
                    TargetTableName = targetTableName,
                    OverwriteExisting = settings.OverwriteExisting
                };

                var itemProgress = progress is null ? null : new OffsetProgress(progress, completedRows);
                var result = await exportJobRunner.RunAsync(itemSettings, itemProgress, cancellationToken);
                items.Add(new BatchExportItemResult(
                    sqlFile,
                    targetTableName,
                    result.Status,
                    result.RowCount,
                    result.Error));

                if (result.Status == ExportStatus.Cancelled)
                {
                    stopwatch.Stop();
                    return CreateResult(ExportStatus.Cancelled, sqlFiles.Length, stopwatch.Elapsed, settings.SqliteFilePath, items);
                }
            }

            stopwatch.Stop();
            var status = items.Any(item => item.Status != ExportStatus.Succeeded)
                ? ExportStatus.Failed
                : ExportStatus.Succeeded;
            return CreateResult(status, sqlFiles.Length, stopwatch.Elapsed, settings.SqliteFilePath, items);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            progress?.Report(new ExportProgress(ExportStatus.Cancelled, items.Sum(item => item.RowCount), "Batch export was cancelled."));
            return CreateResult(ExportStatus.Cancelled, items.Count, stopwatch.Elapsed, null, items);
        }
    }

    private static BatchExportResult FailedValidation(
        Stopwatch stopwatch,
        List<BatchExportItemResult> items,
        string sqlFilePath,
        string message)
    {
        stopwatch.Stop();
        items.Add(new BatchExportItemResult(
            sqlFilePath,
            string.Empty,
            ExportStatus.Failed,
            0,
            new ExportError(ExportErrorCodes.ValidationFailed, message)));

        return CreateResult(ExportStatus.Failed, 0, stopwatch.Elapsed, null, items);
    }

    private static BatchExportResult CreateResult(
        ExportStatus status,
        int totalFiles,
        TimeSpan elapsed,
        string? outputPath,
        IReadOnlyList<BatchExportItemResult> items)
    {
        return new BatchExportResult(
            status,
            totalFiles,
            items.Count(item => item.Status == ExportStatus.Succeeded),
            items.Count(item => item.Status == ExportStatus.Failed),
            items.Where(item => item.Status == ExportStatus.Succeeded).Sum(item => item.RowCount),
            elapsed,
            outputPath,
            items.ToArray());
    }

    private sealed class OffsetProgress(
        IProgress<ExportProgress> inner,
        long rowOffset) : IProgress<ExportProgress>
    {
        public void Report(ExportProgress value)
        {
            inner.Report(value with { RowsWritten = rowOffset + value.RowsWritten });
        }
    }
}
