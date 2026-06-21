using OracleToSqlite.Core.Models;

namespace OracleToSqlite.Core.Services;

public interface ISqliteExportService
{
    Task<long> ExportAsync(
        ExportJobSettings settings,
        OracleQueryResult result,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
