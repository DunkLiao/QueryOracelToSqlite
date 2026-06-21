using FluentAssertions;
using OracleToSqlite.App.ViewModels;
using OracleToSqlite.Core;

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
        var viewModel = new MainViewModel();

        viewModel.Title.Should().Be("Oracle To SQLite");
    }
}
