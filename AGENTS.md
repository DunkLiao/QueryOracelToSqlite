# Repository Guidelines

## Project Structure & Module Organization

This repository is currently in planning/setup stage for a Windows desktop tool that exports Oracle SQL query results into SQLite tables.

- `rfp/PLAN.md`: product and implementation plan.
- `TODO.md`: implementation checklist derived from the plan.
- `init_dev.md`: local development prerequisites and NuGet package list.
- `test/`: test-data preparation notes and downloaded Oracle sample schema archive.

When implementation begins, keep the .NET solution at the repository root. Recommended layout:

- `src/OracleToSqlite.App/`: WPF desktop application.
- `src/OracleToSqlite.Core/`: export workflow, Oracle access, SQLite writing, shared models.
- `tests/OracleToSqlite.Tests/`: unit and integration tests.

## Build, Test, and Development Commands

After the .NET solution is created, use these commands from the repository root:

```powershell
dotnet restore
dotnet build
dotnet test
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

- `dotnet restore`: downloads NuGet dependencies.
- `dotnet build`: compiles the solution.
- `dotnet test`: runs automated tests.
- `dotnet publish`: creates a portable Windows release build.

## Coding Style & Naming Conventions

Use standard C#/.NET conventions:

- Four-space indentation.
- `PascalCase` for public types, methods, properties, and interfaces.
- Prefix interfaces with `I`, for example `IOracleQueryService`.
- Use `camelCase` for local variables and private method parameters.
- Prefer small services with explicit responsibilities, matching the planned `IOracleQueryService`, `ISqliteExportService`, and `IExportJobRunner` boundaries.

Run formatting with:

```powershell
dotnet format
```

## Testing Guidelines

Use xUnit for automated tests, with FluentAssertions where helpful.

Recommended naming:

- Test files: `TypeNameTests.cs`
- Test methods: `MethodName_ShouldExpectedBehavior_WhenCondition`

Prioritize tests for Oracle-to-SQLite type mapping, SQLite identifier quoting, SQL generation, null handling, date/time conversion, and full export flow using mocked Oracle results. Real Oracle tests should be treated as manual or environment-dependent integration checks.

## Commit & Pull Request Guidelines

This directory is not currently initialized as a Git repository, so no historical commit convention exists. Use concise imperative commit messages once Git is initialized, for example:

```text
Add SQLite export service
Validate Oracle connection settings
```

Pull requests should include:

- Summary of user-visible behavior changes.
- Tests run, including manual Oracle/SQLite checks when applicable.
- Screenshots for WPF UI changes.
- Notes about configuration, credentials, or sample database setup.

## Security & Configuration Tips

Do not commit real Oracle credentials, connection strings, exported customer data, or generated SQLite databases. Keep sample data under `test/` and document setup steps instead of storing sensitive runtime files.
