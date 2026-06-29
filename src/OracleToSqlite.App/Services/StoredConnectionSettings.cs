namespace OracleToSqlite.App.Services;

public sealed record StoredConnectionSettings(
    string Host,
    string Port,
    string ServiceName,
    string Username,
    string Password);
