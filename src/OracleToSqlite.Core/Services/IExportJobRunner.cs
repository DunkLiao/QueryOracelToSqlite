using OracleToSqlite.Core.Models;

namespace OracleToSqlite.Core.Services;

public interface IExportJobRunner
{
    Task<ExportResult> RunAsync(
        ExportJobSettings settings,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
