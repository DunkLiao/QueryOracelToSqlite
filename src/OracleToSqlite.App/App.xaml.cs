using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using OracleToSqlite.App.Services;
using OracleToSqlite.App.ViewModels;
using OracleToSqlite.Core.Services;

namespace OracleToSqlite.App;

public partial class App : Application
{
    private ServiceProvider? serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOracleQueryService, OracleQueryService>();
        services.AddSingleton<ISqliteExportService, SqliteExportService>();
        services.AddSingleton<ISqlFileReader, SqlFileReader>();
        services.AddSingleton<IExportJobRunner, ExportJobRunner>();
        services.AddSingleton<IBatchExportJobRunner, BatchExportJobRunner>();
        services.AddSingleton<IFileDialogService, WindowsFileDialogService>();
        services.AddSingleton<IConnectionSettingsStore, WindowsConnectionSettingsStore>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();

        serviceProvider = services.BuildServiceProvider();
        var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
