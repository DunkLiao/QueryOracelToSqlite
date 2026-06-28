using FluentAssertions;
using OracleToSqlite.App.Services;
using OracleToSqlite.App.ViewModels;
using OracleToSqlite.Core.Models;
using OracleToSqlite.Core.Services;

namespace OracleToSqlite.Tests;

public class MainViewModelTests
{
    [Fact]
    public void Constructor_ShouldExposeDefaultFormState()
    {
        var viewModel = new MainViewModel(
            new FakeBatchExportJobRunner(),
            new FakeOracleQueryService(),
            new FakeFileDialogService());

        viewModel.Title.Should().Be("Oracle To SQLite");
        viewModel.Port.Should().Be("1521");
        viewModel.IsRunning.Should().BeFalse();
        viewModel.StatusMessage.Should().Be("Ready.");
        viewModel.RowsWritten.Should().Be(0);
        viewModel.ErrorMessage.Should().BeNull();
        viewModel.RunExportCommand.CanExecute(null).Should().BeTrue();
        viewModel.TestConnectionCommand.CanExecute(null).Should().BeTrue();
        viewModel.CancelExportCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void BrowseOutputPathCommand_ShouldUseDialogResult()
    {
        var dialog = new FakeFileDialogService { Result = @"C:\exports\report.db" };
        var viewModel = new MainViewModel(new FakeBatchExportJobRunner(), new FakeOracleQueryService(), dialog)
        {
            SqliteFilePath = @"C:\exports\old.db"
        };

        viewModel.BrowseOutputPathCommand.Execute(null);

        dialog.CurrentPath.Should().Be(@"C:\exports\old.db");
        viewModel.SqliteFilePath.Should().Be(@"C:\exports\report.db");
    }

    [Fact]
    public void BrowseSqlFolderCommand_ShouldUseDialogResult()
    {
        var dialog = new FakeFileDialogService { FolderResult = @"C:\exports\sql" };
        var viewModel = new MainViewModel(new FakeBatchExportJobRunner(), new FakeOracleQueryService(), dialog)
        {
            SqlFolderPath = @"C:\exports\old-sql"
        };

        viewModel.BrowseSqlFolderCommand.Execute(null);

        dialog.CurrentFolderPath.Should().Be(@"C:\exports\old-sql");
        viewModel.SqlFolderPath.Should().Be(@"C:\exports\sql");
    }

    [Fact]
    public void ClearCommand_ShouldResetEditableFieldsAndStatus()
    {
        var viewModel = CreateValidViewModel();
        viewModel.StatusMessage = "Export completed.";
        viewModel.ErrorMessage = "Previous error";
        viewModel.RowsWritten = 12;
        viewModel.OutputPath = @"C:\exports\report.db";

        viewModel.ClearCommand.Execute(null);

        viewModel.Host.Should().BeEmpty();
        viewModel.Port.Should().Be("1521");
        viewModel.ServiceName.Should().BeEmpty();
        viewModel.Username.Should().BeEmpty();
        viewModel.Password.Should().BeEmpty();
        viewModel.SqlQuery.Should().BeEmpty();
        viewModel.SqlFolderPath.Should().BeEmpty();
        viewModel.SqliteFilePath.Should().BeEmpty();
        viewModel.TargetTableName.Should().BeEmpty();
        viewModel.RowsWritten.Should().Be(0);
        viewModel.OutputPath.Should().BeNull();
        viewModel.ErrorMessage.Should().BeNull();
        viewModel.StatusMessage.Should().Be("Ready.");
    }

    [Fact]
    public async Task RunExportCommand_ShouldShowValidationError_WhenRequiredFieldsAreBlank()
    {
        var runner = new FakeBatchExportJobRunner();
        var viewModel = new MainViewModel(runner, new FakeOracleQueryService(), new FakeFileDialogService());

        await viewModel.RunExportCommand.ExecuteAsync(null);

        runner.Settings.Should().BeNull();
        viewModel.ErrorMessage.Should().Contain("Oracle host is required.");
        viewModel.StatusMessage.Should().Be("Validation failed.");
        viewModel.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task TestConnectionCommand_ShouldPassConnectionSettingsAndShowSuccess()
    {
        var oracle = new FakeOracleQueryService();
        var viewModel = CreateValidViewModel(oracleQueryService: oracle);

        await viewModel.TestConnectionCommand.ExecuteAsync(null);

        oracle.TestedSettings.Should().NotBeNull();
        oracle.TestedSettings!.Host.Should().Be("db.example.local");
        oracle.TestedSettings.Port.Should().Be(1521);
        oracle.TestedSettings.ServiceName.Should().Be("ORCLPDB1");
        oracle.TestedSettings.Username.Should().Be("report_user");
        oracle.TestedSettings.Password.Should().Be("secret");
        viewModel.StatusMessage.Should().Be("Oracle connection succeeded.");
        viewModel.ErrorMessage.Should().BeNull();
        viewModel.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task TestConnectionCommand_ShouldOnlyRequireConnectionFields()
    {
        var oracle = new FakeOracleQueryService();
        var viewModel = new MainViewModel(
            new FakeBatchExportJobRunner(),
            oracle,
            new FakeFileDialogService())
        {
            Host = "db.example.local",
            Port = "1521",
            ServiceName = "ORCLPDB1",
            Username = "report_user",
            Password = "secret"
        };

        await viewModel.TestConnectionCommand.ExecuteAsync(null);

        oracle.TestedSettings.Should().NotBeNull();
        viewModel.StatusMessage.Should().Be("Oracle connection succeeded.");
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task TestConnectionCommand_ShouldShowValidationError_WhenConnectionFieldsAreBlank()
    {
        var oracle = new FakeOracleQueryService();
        var viewModel = new MainViewModel(
            new FakeBatchExportJobRunner(),
            oracle,
            new FakeFileDialogService());

        await viewModel.TestConnectionCommand.ExecuteAsync(null);

        oracle.TestedSettings.Should().BeNull();
        viewModel.StatusMessage.Should().Be("Validation failed.");
        viewModel.ErrorMessage.Should().Contain("Oracle host is required.");
        viewModel.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task TestConnectionCommand_ShouldShowOracleError_WhenConnectionFails()
    {
        var oracle = new FakeOracleQueryService
        {
            TestException = new OracleQueryException(
                new ExportError(
                    ExportErrorCodes.OracleConnectionFailed,
                    "Oracle connection failed while testing Oracle connection.",
                    "Unable to reach Oracle listener."),
                new InvalidOperationException("Unable to reach Oracle listener."))
        };
        var viewModel = CreateValidViewModel(oracleQueryService: oracle);

        await viewModel.TestConnectionCommand.ExecuteAsync(null);

        viewModel.StatusMessage.Should().Be("Oracle connection failed.");
        viewModel.ErrorMessage.Should().Contain("ORACLE_CONNECTION_FAILED");
        viewModel.ErrorMessage.Should().Contain("Unable to reach Oracle listener.");
        viewModel.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task RunExportCommand_ShouldPassBatchSettingsAndShowSuccess()
    {
        var runner = new FakeBatchExportJobRunner
        {
            Result = CreateBatchSuccess(2, 12, @"C:\exports\report.db")
        };
        var viewModel = CreateValidViewModel(runner);

        await viewModel.RunExportCommand.ExecuteAsync(null);

        runner.Settings.Should().NotBeNull();
        runner.Settings!.Connection.Host.Should().Be("db.example.local");
        runner.Settings.Connection.Port.Should().Be(1521);
        runner.Settings.Connection.ServiceName.Should().Be("ORCLPDB1");
        runner.Settings.Connection.Username.Should().Be("report_user");
        runner.Settings.Connection.Password.Should().Be("secret");
        runner.Settings.SqlFolderPath.Should().Be(@"C:\exports\sql");
        runner.Settings.SqliteFilePath.Should().Be(@"C:\exports\report.db");
        viewModel.StatusMessage.Should().Be("Export completed.");
        viewModel.RowsWritten.Should().Be(12);
        viewModel.OutputPath.Should().Be(@"C:\exports\report.db");
        viewModel.ErrorMessage.Should().BeNull();
        viewModel.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task RunExportCommand_ShouldShowCompletedWithErrors_WhenBatchHasPartialFailures()
    {
        var runner = new FakeBatchExportJobRunner
        {
            Result = new BatchExportResult(
                ExportStatus.Failed,
                3,
                2,
                1,
                20,
                TimeSpan.FromSeconds(1),
                @"C:\exports\report.db",
                new[]
                {
                    new BatchExportItemResult(@"C:\exports\sql\Customers.sql", "Customers", ExportStatus.Succeeded, 10, null),
                    new BatchExportItemResult(
                        @"C:\exports\sql\Broken.sql",
                        "Broken",
                        ExportStatus.Failed,
                        0,
                        new ExportError(ExportErrorCodes.OracleSqlFailed, "Oracle SQL failed.", "ORA-00942")),
                    new BatchExportItemResult(@"C:\exports\sql\Orders.sql", "Orders", ExportStatus.Succeeded, 10, null)
                })
        };
        var viewModel = CreateValidViewModel(runner);

        await viewModel.RunExportCommand.ExecuteAsync(null);

        viewModel.StatusMessage.Should().Be("Export completed with errors.");
        viewModel.RowsWritten.Should().Be(20);
        viewModel.OutputPath.Should().Be(@"C:\exports\report.db");
        viewModel.ErrorMessage.Should().Contain("Succeeded: 2");
        viewModel.ErrorMessage.Should().Contain("Failed: 1");
        viewModel.ErrorMessage.Should().Contain("Broken.sql");
    }

    [Fact]
    public async Task RunExportCommand_ShouldShowError_WhenBatchValidationFails()
    {
        var runner = new FakeBatchExportJobRunner
        {
            Result = CreateBatchFailure(
                new ExportError(
                    ExportErrorCodes.ValidationFailed,
                    "SQL folder path is required and must exist."))
        };
        var viewModel = CreateValidViewModel(runner);

        await viewModel.RunExportCommand.ExecuteAsync(null);

        viewModel.StatusMessage.Should().Be("Export failed.");
        viewModel.ErrorMessage.Should().Contain("VALIDATION_FAILED");
        viewModel.ErrorMessage.Should().Contain("SQL folder");
        viewModel.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task CancelExportCommand_ShouldCancelRunningExport()
    {
        var runner = new FakeBatchExportJobRunner
        {
            DelayUntilCancelled = true
        };
        var viewModel = CreateValidViewModel(runner);

        var runTask = viewModel.RunExportCommand.ExecuteAsync(null);
        await runner.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        viewModel.IsRunning.Should().BeTrue();
        viewModel.CancelExportCommand.CanExecute(null).Should().BeTrue();

        viewModel.CancelExportCommand.Execute(null);
        await runTask;

        runner.ObservedCancellation.Should().BeTrue();
        viewModel.StatusMessage.Should().Be("Export cancelled.");
        viewModel.IsRunning.Should().BeFalse();
    }

    private static MainViewModel CreateValidViewModel(
        FakeBatchExportJobRunner? runner = null,
        FakeOracleQueryService? oracleQueryService = null)
    {
        return new MainViewModel(
            runner ?? new FakeBatchExportJobRunner(),
            oracleQueryService ?? new FakeOracleQueryService(),
            new FakeFileDialogService())
        {
            Host = "db.example.local",
            Port = "1521",
            ServiceName = "ORCLPDB1",
            Username = "report_user",
            Password = "secret",
            SqlQuery = "select * from customers",
            SqlFolderPath = @"C:\exports\sql",
            SqliteFilePath = @"C:\exports\report.db",
            TargetTableName = "Customers"
        };
    }

    private static BatchExportResult CreateBatchSuccess(int succeededCount, long totalRows, string outputPath)
    {
        return new BatchExportResult(
            ExportStatus.Succeeded,
            succeededCount,
            succeededCount,
            0,
            totalRows,
            TimeSpan.FromMilliseconds(10),
            outputPath,
            Array.Empty<BatchExportItemResult>());
    }

    private static BatchExportResult CreateBatchFailure(ExportError error)
    {
        return new BatchExportResult(
            ExportStatus.Failed,
            1,
            0,
            1,
            0,
            TimeSpan.FromMilliseconds(10),
            null,
            new[]
            {
                new BatchExportItemResult(@"C:\exports\sql\Broken.sql", "Broken", ExportStatus.Failed, 0, error)
            });
    }

    private sealed class FakeFileDialogService : IFileDialogService
    {
        public string? CurrentPath { get; private set; }

        public string? CurrentFolderPath { get; private set; }

        public string? Result { get; init; }

        public string? FolderResult { get; init; }

        public string? ShowSaveSqliteDialog(string? currentPath)
        {
            CurrentPath = currentPath;
            return Result;
        }

        public string? ShowSelectFolderDialog(string? currentPath)
        {
            CurrentFolderPath = currentPath;
            return FolderResult;
        }
    }

    private sealed class FakeBatchExportJobRunner : IBatchExportJobRunner
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public BatchExportJobSettings? Settings { get; private set; }

        public BatchExportResult Result { get; init; } = CreateBatchSuccess(1, 0, @"C:\exports\report.db");

        public bool DelayUntilCancelled { get; init; }

        public bool ObservedCancellation { get; private set; }

        public async Task<BatchExportResult> RunAsync(
            BatchExportJobSettings settings,
            IProgress<ExportProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Settings = settings;
            progress?.Report(new ExportProgress(ExportStatus.Running, 1, "Writing rows to SQLite."));
            Started.TrySetResult();

            if (DelayUntilCancelled)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    ObservedCancellation = true;
                    return new BatchExportResult(
                        ExportStatus.Cancelled,
                        0,
                        0,
                        0,
                        0,
                        TimeSpan.FromMilliseconds(10),
                        null,
                        Array.Empty<BatchExportItemResult>());
                }
            }

            return Result;
        }
    }

    private sealed class FakeOracleQueryService : IOracleQueryService
    {
        public OracleConnectionSettings? TestedSettings { get; private set; }

        public Exception? TestException { get; init; }

        public Task TestConnectionAsync(
            OracleConnectionSettings settings,
            CancellationToken cancellationToken = default)
        {
            TestedSettings = settings;

            if (TestException is not null)
            {
                throw TestException;
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<OracleColumnSchema>> GetSchemaAsync(
            OracleConnectionSettings settings,
            string sqlQuery,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<OracleQueryResult> ExecuteQueryAsync(
            OracleConnectionSettings settings,
            string sqlQuery,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
