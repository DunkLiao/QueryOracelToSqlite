namespace OracleToSqlite.Core.Services;

public interface ISqlFileReader
{
    Task<SqlFileReadResult> ReadAsync(
        string path,
        CancellationToken cancellationToken = default);
}
