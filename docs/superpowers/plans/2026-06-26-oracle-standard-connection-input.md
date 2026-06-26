# Oracle Standard Connection Input Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the full connection string input path and keep Oracle connection entry to the standard host/port/service name/username/password form.

**Architecture:** The UI will expose only the standard Oracle connection fields. `MainViewModel` will validate and build `OracleConnectionSettings` from those fields only, and the core connection factory and export runner will enforce the same shape. Tests will cover the removed full-string path and the remaining standard path so the behavior stays consistent end to end.

**Tech Stack:** C# .NET 8, WPF, CommunityToolkit.Mvvm, xUnit, FluentAssertions.

---

### Task 1: Remove full connection string state from the view model

**Files:**
- Modify: `src/OracleToSqlite.App/ViewModels/MainViewModel.cs`
- Test: `tests/OracleToSqlite.Tests/MainViewModelTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public void Constructor_ShouldNotExposeFullConnectionStringField()
{
    var viewModel = new MainViewModel(new FakeExportJobRunner(), new FakeFileDialogService());

    viewModel.FullConnectionString.Should().BeNullOrEmpty();
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/OracleToSqlite.Tests/OracleToSqlite.Tests.csproj --filter FullyQualifiedName~MainViewModelTests.Constructor_ShouldNotExposeFullConnectionStringField -v minimal`
Expected: FAIL because `FullConnectionString` is still part of the public surface and non-empty state setup remains.

- [ ] **Step 3: Write minimal implementation**

```csharp
// Remove the FullConnectionString observable property.
// Remove any use of UseFullConnectionString in validation, reset, and settings creation.
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/OracleToSqlite.Tests/OracleToSqlite.Tests.csproj --filter FullyQualifiedName~MainViewModelTests.Constructor_ShouldNotExposeFullConnectionStringField -v minimal`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/OracleToSqlite.App/ViewModels/MainViewModel.cs tests/OracleToSqlite.Tests/MainViewModelTests.cs
git commit -m "refactor: remove full oracle connection input"
```

### Task 2: Collapse Oracle connection factory and runner validation to standard mode only

**Files:**
- Modify: `src/OracleToSqlite.Core/Models/OracleConnectionSettings.cs`
- Modify: `src/OracleToSqlite.Core/Services/OracleConnectionStringFactory.cs`
- Modify: `src/OracleToSqlite.Core/Services/ExportJobRunner.cs`
- Test: `tests/OracleToSqlite.Tests/OracleQueryServiceTests.cs`
- Test: `tests/OracleToSqlite.Tests/ExportJobRunnerTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public void Create_ShouldNotAcceptFullConnectionStringMode()
{
    var settings = new OracleConnectionSettings
    {
        Host = "db.example.local",
        Port = 1521,
        ServiceName = "ORCLPDB1",
        Username = "report_user",
        Password = "secret"
    };

    var connectionString = OracleConnectionStringFactory.Create(settings);

    connectionString.Should().Contain("HOST=db.example.local");
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/OracleToSqlite.Tests/OracleToSqlite.Tests.csproj --filter FullyQualifiedName~OracleQueryServiceTests.Create_ShouldNotAcceptFullConnectionStringMode -v minimal`
Expected: FAIL until the factory no longer branches on `UseFullConnectionString`.

- [ ] **Step 3: Write minimal implementation**

```csharp
// Remove UseFullConnectionString and FullConnectionString from OracleConnectionSettings.
// Make OracleConnectionStringFactory always build from Host/Port/ServiceName/Username/Password.
// Remove the full-string validation branch from ExportJobRunner.
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/OracleToSqlite.Tests/OracleToSqlite.Tests.csproj --filter FullyQualifiedName~OracleQueryServiceTests.Create_ShouldNotAcceptFullConnectionStringMode -v minimal`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/OracleToSqlite.Core/Models/OracleConnectionSettings.cs src/OracleToSqlite.Core/Services/OracleConnectionStringFactory.cs src/OracleToSqlite.Core/Services/ExportJobRunner.cs tests/OracleToSqlite.Tests/OracleQueryServiceTests.cs tests/OracleToSqlite.Tests/ExportJobRunnerTests.cs
git commit -m "fix: standardize oracle connection settings"
```

### Task 3: Remove the full connection string field from the WPF UI

**Files:**
- Modify: `src/OracleToSqlite.App/MainWindow.xaml`
- Modify: `src/OracleToSqlite.App/MainWindow.xaml.cs`
- Test: `tests/OracleToSqlite.Tests/MainViewModelTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public void ClearCommand_ShouldResetStandardConnectionFieldsOnly()
{
    var viewModel = CreateValidViewModel();

    viewModel.ClearCommand.Execute(null);

    viewModel.Host.Should().BeEmpty();
    viewModel.Port.Should().Be("1521");
    viewModel.ServiceName.Should().BeEmpty();
    viewModel.Username.Should().BeEmpty();
    viewModel.Password.Should().BeEmpty();
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/OracleToSqlite.Tests/OracleToSqlite.Tests.csproj --filter FullyQualifiedName~MainViewModelTests.ClearCommand_ShouldResetStandardConnectionFieldsOnly -v minimal`
Expected: FAIL until the clear/reset logic no longer references the removed field.

- [ ] **Step 3: Write minimal implementation**

```xml
<!-- Remove the Use full connection string checkbox and the Full connection string textbox from MainWindow.xaml. -->
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/OracleToSqlite.Tests/OracleToSqlite.Tests.csproj --filter FullyQualifiedName~MainViewModelTests.ClearCommand_ShouldResetStandardConnectionFieldsOnly -v minimal`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/OracleToSqlite.App/MainWindow.xaml src/OracleToSqlite.App/MainWindow.xaml.cs tests/OracleToSqlite.Tests/MainViewModelTests.cs
git commit -m "ui: remove full oracle connection input"
```
