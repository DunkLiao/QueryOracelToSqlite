namespace OracleToSqlite.Core.Models;

public sealed class ExportJobSettings
{
    public required OracleConnectionSettings Connection { get; init; }

    public required string SqlQuery { get; init; }

    public required string SqliteFilePath { get; init; }

    public required string TargetTableName { get; init; }

    public IReadOnlyDictionary<string, string> Parameters { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public bool OverwriteExisting { get; init; } = true;
}
