using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OracleToSqlite.App.Services;
using OracleToSqlite.Core.Models;
using OracleToSqlite.Core.Services;

namespace OracleToSqlite.App.ViewModels;

public partial class MainViewModel(
    IExportJobRunner exportJobRunner,
    IOracleQueryService oracleQueryService,
    IFileDialogService fileDialogService) : ObservableObject
{
    public string Title { get; } = "Oracle To SQLite";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunExportCommand))]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    private string host = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunExportCommand))]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    private string port = "1521";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunExportCommand))]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    private string serviceName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunExportCommand))]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    private string username = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunExportCommand))]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    private string password = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunExportCommand))]
    private string sqlQuery = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunExportCommand))]
    private string sqliteFilePath = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunExportCommand))]
    private string targetTableName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunExportCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelExportCommand))]
    [NotifyCanExecuteChangedFor(nameof(BrowseOutputPathCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearCommand))]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    private bool isRunning;

    [ObservableProperty]
    private string statusMessage = "Ready.";

    [ObservableProperty]
    private long rowsWritten;

    [ObservableProperty]
    private string? outputPath;

    [ObservableProperty]
    private string? errorMessage;

    private CancellationTokenSource? cancellationTokenSource;

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void BrowseOutputPath()
    {
        var selectedPath = fileDialogService.ShowSaveSqliteDialog(SqliteFilePath);
        if (!string.IsNullOrWhiteSpace(selectedPath))
        {
            SqliteFilePath = selectedPath;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunExport))]
    private async Task RunExportAsync()
    {
        ResetRunState();

        var validationError = ValidateForm();
        if (validationError is not null)
        {
            StatusMessage = "Validation failed.";
            ErrorMessage = validationError;
            return;
        }

        IsRunning = true;
        StatusMessage = "Export running.";
        cancellationTokenSource = new CancellationTokenSource();

        try
        {
            var progress = new Progress<ExportProgress>(OnProgressChanged);
            var result = await exportJobRunner.RunAsync(
                CreateSettings(),
                progress,
                cancellationTokenSource.Token);

            ApplyResult(result);
        }
        finally
        {
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
            IsRunning = false;
        }
    }

    private bool CanRunExport()
    {
        return !IsRunning;
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private async Task TestConnectionAsync()
    {
        ResetRunState();

        var validationError = ValidateConnectionFields();
        if (validationError is not null)
        {
            StatusMessage = "Validation failed.";
            ErrorMessage = validationError;
            return;
        }

        IsRunning = true;
        StatusMessage = "Testing Oracle connection.";
        cancellationTokenSource = new CancellationTokenSource();

        try
        {
            await oracleQueryService.TestConnectionAsync(
                CreateConnectionSettings(),
                cancellationTokenSource.Token);

            StatusMessage = "Oracle connection succeeded.";
            ErrorMessage = null;
        }
        catch (OracleQueryException exception)
        {
            StatusMessage = "Oracle connection failed.";
            ErrorMessage = FormatError(exception.Error);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Oracle connection test cancelled.";
        }
        finally
        {
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
            IsRunning = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancelExport))]
    private void CancelExport()
    {
        cancellationTokenSource?.Cancel();
        StatusMessage = "Cancelling export.";
    }

    private bool CanCancelExport()
    {
        return IsRunning;
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void Clear()
    {
        Host = string.Empty;
        Port = "1521";
        ServiceName = string.Empty;
        Username = string.Empty;
        Password = string.Empty;
        SqlQuery = string.Empty;
        SqliteFilePath = string.Empty;
        TargetTableName = string.Empty;
        ResetRunState();
        StatusMessage = "Ready.";
    }

    private bool CanEdit()
    {
        return !IsRunning;
    }

    private void ResetRunState()
    {
        RowsWritten = 0;
        OutputPath = null;
        ErrorMessage = null;
    }

    private string? ValidateForm()
    {
        var connectionValidationError = ValidateConnectionFields();
        if (connectionValidationError is not null)
        {
            return connectionValidationError;
        }

        if (string.IsNullOrWhiteSpace(SqlQuery))
        {
            return "SQL query is required.";
        }

        if (string.IsNullOrWhiteSpace(SqliteFilePath))
        {
            return "SQLite output path is required.";
        }

        if (string.IsNullOrWhiteSpace(TargetTableName))
        {
            return "Target table name is required.";
        }

        return null;
    }

    private string? ValidateConnectionFields()
    {
        if (string.IsNullOrWhiteSpace(Host))
        {
            return "Oracle host is required.";
        }

        if (!int.TryParse(Port, out var parsedPort) || parsedPort <= 0)
        {
            return "Oracle port must be a positive number.";
        }

        if (string.IsNullOrWhiteSpace(ServiceName))
        {
            return "Oracle service name is required.";
        }

        if (string.IsNullOrWhiteSpace(Username))
        {
            return "Oracle username is required.";
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            return "Oracle password is required.";
        }

        return null;
    }

    private ExportJobSettings CreateSettings()
    {
        return new ExportJobSettings
        {
            Connection = CreateConnectionSettings(),
            SqlQuery = SqlQuery.Trim(),
            SqliteFilePath = SqliteFilePath.Trim(),
            TargetTableName = TargetTableName.Trim()
        };
    }

    private OracleConnectionSettings CreateConnectionSettings()
    {
        return new OracleConnectionSettings
        {
            Host = NullIfWhiteSpace(Host),
            Port = int.Parse(Port),
            ServiceName = NullIfWhiteSpace(ServiceName),
            Username = NullIfWhiteSpace(Username),
            Password = Password
        };
    }

    private void OnProgressChanged(ExportProgress progress)
    {
        if (!IsRunning)
        {
            return;
        }

        StatusMessage = progress.Message;
        RowsWritten = progress.RowsWritten;
    }

    private void ApplyResult(ExportResult result)
    {
        RowsWritten = result.RowCount;
        OutputPath = result.OutputPath;

        if (result.Status == ExportStatus.Succeeded)
        {
            StatusMessage = result.RowCount == 0
                ? "Export completed with 0 rows."
                : "Export completed.";
            ErrorMessage = null;
            return;
        }

        if (result.Status == ExportStatus.Cancelled)
        {
            StatusMessage = "Export cancelled.";
            ErrorMessage = result.Error?.Message;
            return;
        }

        StatusMessage = "Export failed.";
        ErrorMessage = FormatError(result.Error);
    }

    private static string? FormatError(ExportError? error)
    {
        if (error is null)
        {
            return "Unknown export error.";
        }

        return string.IsNullOrWhiteSpace(error.Detail)
            ? $"{error.Code}: {error.Message}"
            : $"{error.Code}: {error.Message}{Environment.NewLine}{error.Detail}";
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
