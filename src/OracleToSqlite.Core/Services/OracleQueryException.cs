using OracleToSqlite.Core.Models;

namespace OracleToSqlite.Core.Services;

public sealed class OracleQueryException : Exception
{
    public OracleQueryException(ExportError error, Exception innerException)
        : base(error.Message, innerException)
    {
        Error = error;
    }

    public ExportError Error { get; }
}
