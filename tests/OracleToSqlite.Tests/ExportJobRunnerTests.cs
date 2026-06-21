using FluentAssertions;
using Microsoft.Data.Sqlite;
using OracleToSqlite.Core.Models;
using OracleToSqlite.Core.Services;

namespace OracleToSqlite.Tests;

public class ExportJobRunnerTests
{
    [Fact]
    public async Task RunAsync_ShouldQueryOracleExportSqliteAndReturnSuccess()
    {
        var databasePath = CreateTempDatabasePath();
        var settings = CreateSettings(databasePath);
        var oracleResult = new OracleQueryResult(
            new[]
            {
                new OracleColumnSchema(0, "ID", "NUMBER", "Decimal", 10, 0, false),
                new OracleColumnSchema(1, "NAME", "VARCHAR2", "String", null, null, true)
            },
            new[]
            {
                new Dictionary<string, object?> { ["ID"] = 1, ["NAME"] = "Ada" },
                new Dictionary<string, object?> { ["ID"] = 2, ["NAME"] = "Grace" }
            });
        var progressEvents = new List<ExportProgress>();
        var runner = new ExportJobRunner(
            new FakeOracleQueryService(oracleResult),
            new SqliteExportService());

        var result = await runner.RunAsync(
            settings,
            new Progress<ExportProgress>(progressEvents.Add));

        result.Success.Should().BeTrue();
        result.Status.Should().Be(ExportStatus.Succeeded);
        result.RowCount.Should().Be(2);
        result.OutputPath.Should().Be(databasePath);
        result.Elapsed.Should().BeGreaterThan(TimeSpan.Zero);
        progressEvents.Should().Contain(progress => progress.Status == ExportStatus.Running);
        progressEvents.Should().Contain(progress => progress.Status == ExportStatus.Succeeded && progress.RowsWritten == 2);

        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"Customers\"";
        var count = (long)(await command.ExecuteScalarAsync())!;
        count.Should().Be(2);
    }

    [Fact]
    public async Task RunAsync_ShouldReturnValidationError_WhenSqlQueryIsBlank()
    {
        var settings = CreateSettings(CreateTempDatabasePath(), sqlQuery: " ");
        var runner = new ExportJobRunner(
            new FakeOracleQueryService(CreateEmptyResult()),
            new SqliteExportService());

        var result = await runner.RunAsync(settings);

        result.Status.Should().Be(ExportStatus.Failed);
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be(ExportErrorCodes.ValidationFailed);
        result.Error.Message.Should().Contain("SQL query");
    }

    [Fact]
    public async Task RunAsync_ShouldPreserveOracleError_WhenOracleQueryFails()
    {
        var settings = CreateSettings(CreateTempDatabasePath());
        var oracleError = new ExportError(
            ExportErrorCodes.OracleSqlFailed,
            "Oracle SQL failed while executing Oracle query.",
            "ORA-00942: table or view does not exist");
        var runner = new ExportJobRunner(
            new FakeOracleQueryService(CreateEmptyResult(), new OracleQueryException(oracleError, new InvalidOperationException("ORA-00942"))),
            new SqliteExportService());

        var result = await runner.RunAsync(settings);

        result.Status.Should().Be(ExportStatus.Failed);
        result.Error.Should().Be(oracleError);
    }

    [Fact]
    public async Task RunAsync_ShouldReturnCancelled_WhenCancellationIsRequestedBeforeStart()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        var runner = new ExportJobRunner(
            new FakeOracleQueryService(CreateEmptyResult()),
            new SqliteExportService());

        var result = await runner.RunAsync(
            CreateSettings(CreateTempDatabasePath()),
            cancellationToken: cancellationTokenSource.Token);

        result.Status.Should().Be(ExportStatus.Cancelled);
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be(ExportErrorCodes.ExportCancelled);
    }

    private static ExportJobSettings CreateSettings(
        string databasePath,
        string sqlQuery = "select id, name from customers")
    {
        return new ExportJobSettings
        {
            Connection = new OracleConnectionSettings
            {
                Host = "db.example.local",
                ServiceName = "ORCLPDB1",
                Username = "user",
                Password = "password"
            },
            SqlQuery = sqlQuery,
            SqliteFilePath = databasePath,
            TargetTableName = "Customers"
        };
    }

    private static string CreateTempDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), $"oracle-to-sqlite-runner-{Guid.NewGuid():N}.db");
    }

    private static OracleQueryResult CreateEmptyResult()
    {
        return new OracleQueryResult(Array.Empty<OracleColumnSchema>(), Array.Empty<IReadOnlyDictionary<string, object?>>());
    }

    private sealed class FakeOracleQueryService(
        OracleQueryResult result,
        Exception? exception = null) : IOracleQueryService
    {
        public Task TestConnectionAsync(
            OracleConnectionSettings settings,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<OracleColumnSchema>> GetSchemaAsync(
            OracleConnectionSettings settings,
            string sqlQuery,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(result.Columns);
        }

        public Task<OracleQueryResult> ExecuteQueryAsync(
            OracleConnectionSettings settings,
            string sqlQuery,
            CancellationToken cancellationToken = default)
        {
            if (exception is not null)
            {
                throw exception;
            }

            return Task.FromResult(result);
        }
    }
}
