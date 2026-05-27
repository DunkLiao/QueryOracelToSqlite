# Query Oracle To SQLite

Windows desktop tool for exporting Oracle SQL query results directly into SQLite database tables.

This repository is currently in planning and setup stage. The intended implementation stack is **C# .NET 8 + WPF + ODP.NET Managed + Microsoft.Data.Sqlite**.

## Goals

- Provide a simple desktop UI for entering Oracle connection settings and SQL queries.
- Export query results into a local SQLite `.db` file.
- Create or overwrite the target SQLite table automatically.
- Avoid requiring Oracle Client installation by using `Oracle.ManagedDataAccess`.
- Publish as a self-contained Windows application that can run without installing .NET Runtime.

## Current Repository Structure

```text
.
├── AGENTS.md
├── README.md
├── TODO.md
├── init_dev.md
├── rfp/
│   └── PLAN.md
└── test/
    ├── gen_sample_oracle_db.md
    └── oracle-db-sample-schemas-main.zip
```

## Documentation

- `rfp/PLAN.md`: implementation plan and first-version scope.
- `TODO.md`: executable task checklist.
- `init_dev.md`: required developer tools and NuGet packages.
- `test/gen_sample_oracle_db.md`: notes for preparing free Oracle sample schemas.
- `AGENTS.md`: contributor and agent guidelines.

## Planned Technology Stack

- UI: WPF on .NET 8
- Architecture: MVVM
- Oracle driver: `Oracle.ManagedDataAccess`
- SQLite driver: `Microsoft.Data.Sqlite`
- MVVM helpers: `CommunityToolkit.Mvvm`
- Testing: xUnit and FluentAssertions

## Development Setup

Install the required tools listed in `init_dev.md`:

- .NET 8 SDK
- Visual Studio 2022 Community with `.NET desktop development`
- Git

After the solution is created, the expected commands will be:

```powershell
dotnet restore
dotnet build
dotnet test
```

To create a self-contained Windows build:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

## Testing Data

Oracle sample schemas can be downloaded from the official Oracle sample repository:

- https://github.com/oracle-samples/db-sample-schemas

A downloaded archive is stored under `test/` for setup reference. Start with the `HR` schema for basic connection and export tests, then use larger schemas such as `SH` for volume testing.

## Status

No application source code has been generated yet. The next major step is to create the .NET WPF solution and implement the services described in `rfp/PLAN.md`.
