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
        var viewModel = new MainViewModel(new FakeExportJobRunner(), new FakeFileDialogService());

        viewModel.Title.Should().Be("Oracle To SQLite");
        viewModel.Port.Should().Be("1521");
        viewModel.IsRunning.Should().BeFalse();
        viewModel.StatusMessage.Should().Be("Ready.");
        viewModel.RowsWritten.Should().Be(0);
        viewModel.ErrorMessage.Should().BeNull();
        viewModel.RunExportCommand.CanExecute(null).Should().BeTrue();
        viewModel.CancelExportCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void BrowseOutputPathCommand_ShouldUseDialogResult()
    {
        var dialog = new FakeFileDialogService { Result = @"C:\exports\report.db" };
        var viewModel = new MainViewModel(new FakeExportJobRunner(), dialog)
        {
            SqliteFilePath = @"C:\exports\old.db"
        };

        viewModel.BrowseOutputPathCommand.Execute(null);

        dialog.CurrentPath.Should().Be(@"C:\exports\old.db");
        viewModel.SqliteFilePath.Should().Be(@"C:\exports\report.db");
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
        var runner = new FakeExportJobRunner();
        var viewModel = new MainViewModel(runner, new FakeFileDialogService());

        await viewModel.RunExportCommand.ExecuteAsync(null);

        runner.Settings.Should().BeNull();
        viewModel.ErrorMessage.Should().Contain("Oracle host is required.");
        viewModel.StatusMessage.Should().Be("Validation failed.");
        viewModel.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task RunExportCommand_ShouldPassSettingsAndShowSuccess()
    {
        var runner = new FakeExportJobRunner
        {
            Result = ExportResult.Succeeded(2, TimeSpan.FromSeconds(1), @"C:\exports\report.db")
        };
        var viewModel = CreateValidViewModel(runner);

        await viewModel.RunExportCommand.ExecuteAsync(null);

        runner.Settings.Should().NotBeNull();
        runner.Settings!.Connection.Host.Should().Be("db.example.local");
        runner.Settings.Connection.Port.Should().Be(1521);
        runner.Settings.Connection.ServiceName.Should().Be("ORCLPDB1");
        runner.Settings.Connection.Username.Should().Be("report_user");
        runner.Settings.Connection.Password.Should().Be("secret");
        runner.Settings.SqlQuery.Should().Be("select * from customers");
        runner.Settings.SqliteFilePath.Should().Be(@"C:\exports\report.db");
        runner.Settings.TargetTableName.Should().Be("Customers");
        viewModel.StatusMessage.Should().Be("Export completed.");
        viewModel.RowsWritten.Should().Be(2);
        viewModel.OutputPath.Should().Be(@"C:\exports\report.db");
        viewModel.ErrorMessage.Should().BeNull();
        viewModel.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task RunExportCommand_ShouldShowError_WhenRunnerFails()
    {
        var runner = new FakeExportJobRunner
        {
            Result = ExportResult.Failed(
                TimeSpan.FromMilliseconds(10),
                new ExportError(
                    ExportErrorCodes.SqliteExportFailed,
                    "SQLite export failed.",
                    "The process cannot access the file."))
        };
        var viewModel = CreateValidViewModel(runner);

        await viewModel.RunExportCommand.ExecuteAsync(null);

        viewModel.StatusMessage.Should().Be("Export failed.");
        viewModel.ErrorMessage.Should().Contain("SQLITE_EXPORT_FAILED");
        viewModel.ErrorMessage.Should().Contain("SQLite export failed.");
        viewModel.ErrorMessage.Should().Contain("The process cannot access the file.");
        viewModel.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task CancelExportCommand_ShouldCancelRunningExport()
    {
        var runner = new FakeExportJobRunner
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

    private static MainViewModel CreateValidViewModel(FakeExportJobRunner? runner = null)
    {
        return new MainViewModel(runner ?? new FakeExportJobRunner(), new FakeFileDialogService())
        {
            Host = "db.example.local",
            Port = "1521",
            ServiceName = "ORCLPDB1",
            Username = "report_user",
            Password = "secret",
            SqlQuery = "select * from customers",
            SqliteFilePath = @"C:\exports\report.db",
            TargetTableName = "Customers"
        };
    }

    private sealed class FakeFileDialogService : IFileDialogService
    {
        public string? CurrentPath { get; private set; }

        public string? Result { get; init; }

        public string? ShowSaveSqliteDialog(string? currentPath)
        {
            CurrentPath = currentPath;
            return Result;
        }
    }

    private sealed class FakeExportJobRunner : IExportJobRunner
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ExportJobSettings? Settings { get; private set; }

        public ExportResult Result { get; init; } = ExportResult.Succeeded(0, TimeSpan.Zero, @"C:\exports\report.db");

        public bool DelayUntilCancelled { get; init; }

        public bool ObservedCancellation { get; private set; }

        public async Task<ExportResult> RunAsync(
            ExportJobSettings settings,
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
                    return new ExportResult(
                        ExportStatus.Cancelled,
                        0,
                        TimeSpan.FromMilliseconds(10),
                        null,
                        new ExportError(ExportErrorCodes.ExportCancelled, "Export was cancelled."));
                }
            }

            return Result;
        }
    }
}
