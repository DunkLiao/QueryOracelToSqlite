namespace OracleToSqlite.Core.Models;

public sealed class OracleConnectionSettings
{
    public string? Host { get; init; }

    public int Port { get; init; } = 1521;

    public string? ServiceName { get; init; }

    public string? Username { get; init; }

    public string? Password { get; init; }
}
