using FluentAssertions;
using OracleToSqlite.Core.Models;
using OracleToSqlite.Core.Services;

namespace OracleToSqlite.Tests;

public class BatchExportJobRunnerTests
{
    [Fact]
    public async Task RunAsync_ShouldExportTopLevelSqlFilesUsingFileNamesAsTableNames()
    {
        var folderPath = CreateTempFolder();
        var databasePath = CreateTempDatabasePath();
        await File.WriteAllTextAsync(Path.Combine(folderPath, "Customers.sql"), "select * from customers");
        await File.WriteAllTextAsync(Path.Combine(folderPath, "Orders.sql"), "select * from orders");
        await File.WriteAllTextAsync(Path.Combine(folderPath, "notes.txt"), "not sql");
        Directory.CreateDirectory(Path.Combine(folderPath, "nested"));
        await File.WriteAllTextAsync(Path.Combine(folderPath, "nested", "Ignored.sql"), "select * from ignored");
        var singleRunner = new FakeExportJobRunner();
        var runner = new BatchExportJobRunner(singleRunner);

        var result = await runner.RunAsync(CreateSettings(folderPath, databasePath));

        result.Status.Should().Be(ExportStatus.Succeeded);
        result.Success.Should().BeTrue();
        result.TotalFiles.Should().Be(2);
        result.SucceededCount.Should().Be(2);
        result.FailedCount.Should().Be(0);
        result.TotalRows.Should().Be(10);
        result.OutputPath.Should().Be(databasePath);
        singleRunner.Settings.Should().HaveCount(2);
        singleRunner.Settings.Select(settings => settings.SqlQuery)
            .Should().BeEquivalentTo("select * from customers", "select * from orders");
        singleRunner.Settings.Select(settings => settings.TargetTableName)
            .Should().BeEquivalentTo("Customers", "Orders");
        singleRunner.Settings.Should().OnlyContain(settings => settings.SqliteFilePath == databasePath);
    }

    [Fact]
    public async Task RunAsync_ShouldContinueWithRemainingFiles_WhenOneSqlFileFails()
    {
        var folderPath = CreateTempFolder();
        var databasePath = CreateTempDatabasePath();
        await File.WriteAllTextAsync(Path.Combine(folderPath, "Customers.sql"), "select * from customers");
        await File.WriteAllTextAsync(Path.Combine(folderPath, "Broken.sql"), "select * from missing_table");
        await File.WriteAllTextAsync(Path.Combine(folderPath, "Orders.sql"), "select * from orders");
        var singleRunner = new FakeExportJobRunner
        {
            FailTableName = "Broken"
        };
        var runner = new BatchExportJobRunner(singleRunner);

        var result = await runner.RunAsync(CreateSettings(folderPath, databasePath));

        result.Status.Should().Be(ExportStatus.Failed);
        result.Success.Should().BeFalse();
        result.TotalFiles.Should().Be(3);
        result.SucceededCount.Should().Be(2);
        result.FailedCount.Should().Be(1);
        result.TotalRows.Should().Be(10);
        result.Items.Should().Contain(item =>
            item.TargetTableName == "Broken" &&
            item.Status == ExportStatus.Failed &&
            item.Error!.Code == ExportErrorCodes.OracleSqlFailed);
        singleRunner.Settings.Select(settings => settings.TargetTableName)
            .Should().BeEquivalentTo("Broken", "Customers", "Orders");
    }

    [Fact]
    public async Task RunAsync_ShouldPassParametersToEachSqlFile()
    {
        var folderPath = CreateTempFolder();
        var databasePath = CreateTempDatabasePath();
        await File.WriteAllTextAsync(Path.Combine(folderPath, "Customers.sql"), "select * from customers where ss_seq = :THIS_MONTH_SS_SEQ");
        await File.WriteAllTextAsync(Path.Combine(folderPath, "Orders.sql"), "select * from orders where ss_seq = &THIS_MONTH_SS_SEQ");
        var singleRunner = new FakeExportJobRunner();
        var runner = new BatchExportJobRunner(singleRunner);

        var result = await runner.RunAsync(CreateSettings(
            folderPath,
            databasePath,
            new Dictionary<string, string> { ["THIS_MONTH_SS_SEQ"] = "202405" }));

        result.Status.Should().Be(ExportStatus.Succeeded);
        singleRunner.Settings.Should().HaveCount(2);
        singleRunner.Settings.Should().OnlyContain(settings =>
            settings.Parameters["THIS_MONTH_SS_SEQ"] == "202405");
    }

    [Fact]
    public async Task RunAsync_ShouldReportCumulativeRowsAcrossSqlFiles()
    {
        var folderPath = CreateTempFolder();
        var databasePath = CreateTempDatabasePath();
        await File.WriteAllTextAsync(Path.Combine(folderPath, "Customers.sql"), "select * from customers");
        await File.WriteAllTextAsync(Path.Combine(folderPath, "Orders.sql"), "select * from orders");
        var progressEvents = new List<ExportProgress>();
        var runner = new BatchExportJobRunner(new FakeExportJobRunner
        {
            ReportProgress = true
        });

        await runner.RunAsync(
            CreateSettings(folderPath, databasePath),
            new CollectingProgress<ExportProgress>(progressEvents));

        progressEvents.Select(progress => progress.RowsWritten).Should().ContainInOrder(5, 10);
    }

    [Fact]
    public async Task RunAsync_ShouldReportDetectedSqlFileEncoding()
    {
        var folderPath = CreateTempFolder();
        var databasePath = CreateTempDatabasePath();
        await File.WriteAllTextAsync(Path.Combine(folderPath, "Customers.sql"), "select * from customers");
        var progressEvents = new List<ExportProgress>();
        var runner = new BatchExportJobRunner(
            new FakeExportJobRunner(),
            new FakeSqlFileReader("select * from customers", "UTF-8"));

        await runner.RunAsync(
            CreateSettings(folderPath, databasePath),
            new CollectingProgress<ExportProgress>(progressEvents));

        progressEvents.Select(progress => progress.Message)
            .Should().Contain(message => message == "Reading Customers.sql as UTF-8.");
    }

    [Fact]
    public async Task RunAsync_ShouldReportEachSqlFileResult()
    {
        var folderPath = CreateTempFolder();
        var databasePath = CreateTempDatabasePath();
        await File.WriteAllTextAsync(Path.Combine(folderPath, "Customers.sql"), "select * from customers");
        await File.WriteAllTextAsync(Path.Combine(folderPath, "Broken.sql"), "select * from missing_table");
        var progressEvents = new List<ExportProgress>();
        var runner = new BatchExportJobRunner(
            new FakeExportJobRunner { FailTableName = "Broken" },
            new FakeSqlFileReader("select * from customers", "UTF-8"));

        await runner.RunAsync(
            CreateSettings(folderPath, databasePath),
            new CollectingProgress<ExportProgress>(progressEvents));

        progressEvents.Select(progress => progress.Message)
            .Should().Contain(message => message == "Completed Customers.sql: 5 rows.");
        progressEvents.Select(progress => progress.Message)
            .Should().Contain(message => message == "Failed Broken.sql: ORACLE_SQL_FAILED.");
    }

    [Fact]
    public async Task RunAsync_ShouldReturnValidationFailure_WhenSqlFolderDoesNotExist()
    {
        var missingFolder = Path.Combine(Path.GetTempPath(), $"oracle-to-sqlite-missing-{Guid.NewGuid():N}");
        var runner = new BatchExportJobRunner(new FakeExportJobRunner());

        var result = await runner.RunAsync(CreateSettings(missingFolder, CreateTempDatabasePath()));

        result.Status.Should().Be(ExportStatus.Failed);
        result.Success.Should().BeFalse();
        result.TotalFiles.Should().Be(0);
        result.FailedCount.Should().Be(1);
        result.Items.Single().Error!.Code.Should().Be(ExportErrorCodes.ValidationFailed);
        result.Items.Single().Error!.Message.Should().Contain("SQL folder");
    }

    private static BatchExportJobSettings CreateSettings(
        string folderPath,
        string databasePath,
        IReadOnlyDictionary<string, string>? parameters = null)
    {
        return new BatchExportJobSettings
        {
            Connection = new OracleConnectionSettings
            {
                Host = "db.example.local",
                ServiceName = "ORCLPDB1",
                Username = "user",
                Password = "password"
            },
            SqlFolderPath = folderPath,
            SqliteFilePath = databasePath,
            Parameters = parameters ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };
    }

    private static string CreateTempFolder()
    {
        var folderPath = Path.Combine(Path.GetTempPath(), $"oracle-to-sqlite-batch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folderPath);
        return folderPath;
    }

    private static string CreateTempDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), $"oracle-to-sqlite-batch-{Guid.NewGuid():N}.db");
    }

    private sealed class FakeExportJobRunner : IExportJobRunner
    {
        public List<ExportJobSettings> Settings { get; } = [];

        public string? FailTableName { get; init; }

        public bool ReportProgress { get; init; }

        public Task<ExportResult> RunAsync(
            ExportJobSettings settings,
            IProgress<ExportProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Settings.Add(settings);

            if (ReportProgress)
            {
                progress?.Report(new ExportProgress(ExportStatus.Running, 5, $"Writing {settings.TargetTableName}."));
            }

            if (settings.TargetTableName == FailTableName)
            {
                return Task.FromResult(ExportResult.Failed(
                    TimeSpan.FromMilliseconds(10),
                    new ExportError(
                        ExportErrorCodes.OracleSqlFailed,
                        "Oracle SQL failed.",
                        "ORA-00942")));
            }

            return Task.FromResult(ExportResult.Succeeded(5, TimeSpan.FromMilliseconds(10), settings.SqliteFilePath));
        }
    }

    private sealed class FakeSqlFileReader(string sqlText, string encodingName) : ISqlFileReader
    {
        public Task<SqlFileReadResult> ReadAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SqlFileReadResult(sqlText, encodingName));
        }
    }

    private sealed class CollectingProgress<T>(ICollection<T> values) : IProgress<T>
    {
        public void Report(T value)
        {
            values.Add(value);
        }
    }
}
