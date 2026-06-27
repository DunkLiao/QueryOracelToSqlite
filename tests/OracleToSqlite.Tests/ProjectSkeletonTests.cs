using FluentAssertions;
using OracleToSqlite.App.Services;
using OracleToSqlite.App.ViewModels;
using OracleToSqlite.Core;
using OracleToSqlite.Core.Models;
using OracleToSqlite.Core.Services;

namespace OracleToSqlite.Tests;

public class ProjectSkeletonTests
{
    [Fact]
    public void CoreAssemblyMarker_ShouldExposeCoreAssemblyName()
    {
        CoreAssemblyMarker.AssemblyName.Should().Be("OracleToSqlite.Core");
    }

    [Fact]
    public void MainViewModel_ShouldExposeApplicationTitle()
    {
        var viewModel = new MainViewModel(
            new FakeExportJobRunner(),
            new FakeOracleQueryService(),
            new FakeFileDialogService());

        viewModel.Title.Should().Be("Oracle To SQLite");
    }

    private sealed class FakeFileDialogService : IFileDialogService
    {
        public string? ShowSaveSqliteDialog(string? currentPath)
        {
            return null;
        }
    }

    private sealed class FakeExportJobRunner : IExportJobRunner
    {
        public Task<ExportResult> RunAsync(
            ExportJobSettings settings,
            IProgress<ExportProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ExportResult.Succeeded(0, TimeSpan.Zero, settings.SqliteFilePath));
        }
    }

    private sealed class FakeOracleQueryService : IOracleQueryService
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
