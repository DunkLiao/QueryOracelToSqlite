using OracleToSqlite.Core.Models;

namespace OracleToSqlite.Core.Services;

public interface IBatchExportJobRunner
{
    Task<BatchExportResult> RunAsync(
        BatchExportJobSettings settings,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
