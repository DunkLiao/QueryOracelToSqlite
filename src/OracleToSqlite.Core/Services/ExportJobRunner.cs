using System.Diagnostics;
using Microsoft.Data.Sqlite;
using OracleToSqlite.Core.Models;

namespace OracleToSqlite.Core.Services;

public sealed class ExportJobRunner(
    IOracleQueryService oracleQueryService,
    ISqliteExportService sqliteExportService) : IExportJobRunner
{
    public async Task<ExportResult> RunAsync(
        ExportJobSettings settings,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Validate(settings);

            progress?.Report(new ExportProgress(ExportStatus.Running, 0, "Executing Oracle query."));
            var preparedQuery = SqlQueryPreprocessor.Prepare(settings.SqlQuery, settings.Parameters);
            var preparedParameters = preparedQuery.Parameters.ToDictionary(
                parameter => parameter.Name,
                parameter => parameter.Value,
                StringComparer.OrdinalIgnoreCase);
            var queryResult = await oracleQueryService.ExecuteQueryAsync(
                settings.Connection,
                preparedQuery.SqlQuery,
                preparedParameters,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new ExportProgress(ExportStatus.Running, 0, "Exporting rows to SQLite."));
            var rowsWritten = await sqliteExportService.ExportAsync(
                settings,
                queryResult,
                progress,
                cancellationToken);

            stopwatch.Stop();
            progress?.Report(new ExportProgress(ExportStatus.Succeeded, rowsWritten, "Export completed."));
            return ExportResult.Succeeded(rowsWritten, stopwatch.Elapsed, settings.SqliteFilePath);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            var error = new ExportError(
                ExportErrorCodes.ExportCancelled,
                "Export was cancelled.");
            progress?.Report(new ExportProgress(ExportStatus.Cancelled, 0, error.Message));
            return new ExportResult(ExportStatus.Cancelled, 0, stopwatch.Elapsed, null, error);
        }
        catch (OracleQueryException exception)
        {
            stopwatch.Stop();
            return ExportResult.Failed(stopwatch.Elapsed, exception.Error);
        }
        catch (ArgumentException exception)
        {
            stopwatch.Stop();
            return ExportResult.Failed(
                stopwatch.Elapsed,
                new ExportError(
                    ExportErrorCodes.ValidationFailed,
                    exception.Message,
                    exception.ParamName));
        }
        catch (Exception exception) when (exception is SqliteException or IOException or InvalidOperationException)
        {
            stopwatch.Stop();
            return ExportResult.Failed(
                stopwatch.Elapsed,
                new ExportError(
                    ExportErrorCodes.SqliteExportFailed,
                    "SQLite export failed.",
                    exception.Message));
        }
    }

    private static void Validate(ExportJobSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(settings.Connection);

        if (string.IsNullOrWhiteSpace(settings.SqlQuery))
        {
            throw new ArgumentException("SQL query is required.", nameof(settings.SqlQuery));
        }

        if (string.IsNullOrWhiteSpace(settings.SqliteFilePath))
        {
            throw new ArgumentException("SQLite output path is required.", nameof(settings.SqliteFilePath));
        }

        if (string.IsNullOrWhiteSpace(settings.TargetTableName))
        {
            throw new ArgumentException("Target table name is required.", nameof(settings.TargetTableName));
        }

        ValidateConnection(settings.Connection);
    }

    private static void ValidateConnection(OracleConnectionSettings connection)
    {
        if (string.IsNullOrWhiteSpace(connection.Host))
        {
            throw new ArgumentException("Host is required.", nameof(connection.Host));
        }

        if (connection.Port <= 0)
        {
            throw new ArgumentException("Port must be greater than zero.", nameof(connection.Port));
        }

        if (string.IsNullOrWhiteSpace(connection.ServiceName))
        {
            throw new ArgumentException("Service name is required.", nameof(connection.ServiceName));
        }

        if (string.IsNullOrWhiteSpace(connection.Username))
        {
            throw new ArgumentException("Username is required.", nameof(connection.Username));
        }

        if (string.IsNullOrWhiteSpace(connection.Password))
        {
            throw new ArgumentException("Password is required.", nameof(connection.Password));
        }
    }
}
