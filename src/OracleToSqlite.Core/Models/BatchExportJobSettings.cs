namespace OracleToSqlite.Core.Models;

public sealed class BatchExportJobSettings
{
    public required OracleConnectionSettings Connection { get; init; }

    public required string SqlFolderPath { get; init; }

    public required string SqliteFilePath { get; init; }

    public bool OverwriteExisting { get; init; } = true;
}
