using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using OracleToSqlite.App.Services;
using OracleToSqlite.Core.Models;
using OracleToSqlite.Core.Services;

namespace OracleToSqlite.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IBatchExportJobRunner batchExportJobRunner;
    private readonly IOracleQueryService oracleQueryService;
    private readonly IFileDialogService fileDialogService;
    private readonly IConnectionSettingsStore connectionSettingsStore;

    public string Title { get; } = "Oracle To SQLite";

    public MainViewModel(
        IBatchExportJobRunner batchExportJobRunner,
        IOracleQueryService oracleQueryService,
        IFileDialogService fileDialogService,
        IConnectionSettingsStore connectionSettingsStore)
    {
        this.batchExportJobRunner = batchExportJobRunner;
        this.oracleQueryService = oracleQueryService;
        this.fileDialogService = fileDialogService;
        this.connectionSettingsStore = connectionSettingsStore;

        LoadSavedConnectionSettings();
    }

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
    private string sqlFolderPath = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunExportCommand))]
    private string sqliteFilePath = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunExportCommand))]
    private string sqlParameters = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunExportCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelExportCommand))]
    [NotifyCanExecuteChangedFor(nameof(BrowseOutputPathCommand))]
    [NotifyCanExecuteChangedFor(nameof(BrowseSqlFolderCommand))]
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

    [ObservableProperty]
    private string runLog = string.Empty;

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

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void BrowseSqlFolder()
    {
        var selectedPath = fileDialogService.ShowSelectFolderDialog(SqlFolderPath);
        if (!string.IsNullOrWhiteSpace(selectedPath))
        {
            SqlFolderPath = selectedPath;
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
            AppendRunLog(validationError);
            return;
        }

        IsRunning = true;
        StatusMessage = "Export running.";
        AppendRunLog("Export running.");
        cancellationTokenSource = new CancellationTokenSource();
        SaveConnectionSettings();

        try
        {
            var progress = new InlineProgress<ExportProgress>(OnProgressChanged);
            var result = await batchExportJobRunner.RunAsync(
                CreateSettings(),
                progress,
                cancellationTokenSource.Token);

            IsRunning = false;
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
            AppendRunLog(validationError);
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
            AppendRunLog("Oracle connection succeeded.");
            SaveConnectionSettings();
        }
        catch (OracleQueryException exception)
        {
            StatusMessage = "Oracle connection failed.";
            ErrorMessage = FormatError(exception.Error);
            AppendRunLog(ErrorMessage);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Oracle connection test cancelled.";
            AppendRunLog(StatusMessage);
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
        SqlFolderPath = string.Empty;
        SqliteFilePath = string.Empty;
        SqlParameters = string.Empty;
        connectionSettingsStore.Clear();
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
        RunLog = string.Empty;
    }

    private string? ValidateForm()
    {
        var connectionValidationError = ValidateConnectionFields();
        if (connectionValidationError is not null)
        {
            return connectionValidationError;
        }

        if (string.IsNullOrWhiteSpace(SqlFolderPath))
        {
            return "SQL folder path is required.";
        }

        if (string.IsNullOrWhiteSpace(SqliteFilePath))
        {
            return "SQLite output path is required.";
        }

        try
        {
            _ = SqlQueryPreprocessor.ParseParameterText(SqlParameters);
        }
        catch (ArgumentException exception)
        {
            return exception.Message;
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

    private BatchExportJobSettings CreateSettings()
    {
        return new BatchExportJobSettings
        {
            Connection = CreateConnectionSettings(),
            SqlFolderPath = SqlFolderPath.Trim(),
            SqliteFilePath = SqliteFilePath.Trim(),
            Parameters = SqlQueryPreprocessor.ParseParameterText(SqlParameters)
        };
    }

    private void LoadSavedConnectionSettings()
    {
        var savedSettings = connectionSettingsStore.Load();
        if (savedSettings is null)
        {
            return;
        }

        Host = savedSettings.Host;
        Port = string.IsNullOrWhiteSpace(savedSettings.Port) ? "1521" : savedSettings.Port;
        ServiceName = savedSettings.ServiceName;
        Username = savedSettings.Username;
        Password = savedSettings.Password;
    }

    private void SaveConnectionSettings()
    {
        connectionSettingsStore.Save(new StoredConnectionSettings(
            Host.Trim(),
            Port.Trim(),
            ServiceName.Trim(),
            Username.Trim(),
            Password));
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
        AppendRunLog(progress.Message);
    }

    private void ApplyResult(BatchExportResult result)
    {
        RowsWritten = result.TotalRows;
        OutputPath = result.OutputPath;

        if (result.Status == ExportStatus.Succeeded)
        {
            StatusMessage = result.TotalRows == 0
                ? "Export completed with 0 rows."
                : "Export completed.";
            ErrorMessage = null;
            AppendRunLog(StatusMessage);
            return;
        }

        if (result.Status == ExportStatus.Failed && result.SucceededCount > 0)
        {
            StatusMessage = "Export completed with errors.";
            ErrorMessage = FormatBatchErrors(result);
            AppendRunLog(StatusMessage);
            AppendRunLog(ErrorMessage);
            return;
        }

        if (result.Status == ExportStatus.Cancelled)
        {
            StatusMessage = "Export cancelled.";
            ErrorMessage = FormatBatchErrors(result);
            AppendRunLog(StatusMessage);
            AppendRunLog(ErrorMessage);
            return;
        }

        StatusMessage = "Export failed.";
        ErrorMessage = FormatBatchErrors(result);
        AppendRunLog(StatusMessage);
        AppendRunLog(ErrorMessage);
    }

    private void AppendRunLog(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        RunLog = string.IsNullOrWhiteSpace(RunLog)
            ? message
            : $"{RunLog}{Environment.NewLine}{message}";
    }

    private static string FormatBatchErrors(BatchExportResult result)
    {
        var failedItems = result.Items
            .Where(item => item.Status == ExportStatus.Failed && item.Error is not null)
            .Select(item => $"{Path.GetFileName(item.SqlFilePath)}: {FormatError(item.Error)}")
            .ToArray();

        var summary = $"Succeeded: {result.SucceededCount}{Environment.NewLine}Failed: {result.FailedCount}";
        return failedItems.Length == 0
            ? summary
            : $"{summary}{Environment.NewLine}{string.Join(Environment.NewLine, failedItems)}";
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

    private sealed class InlineProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value)
        {
            handler(value);
        }
    }
}
