using Oracle.ManagedDataAccess.Client;
using OracleToSqlite.Core.Models;

namespace OracleToSqlite.Core.Services;

public static class OracleErrorMapper
{
    public static ExportError ToExportError(Exception exception, string operation)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (IsAuthenticationFailure(exception))
        {
            return new ExportError(
                ExportErrorCodes.OracleAuthenticationFailed,
                $"Oracle authentication failed while {operation}.",
                exception.Message);
        }

        if (IsConnectionOperation(operation))
        {
            return new ExportError(
                ExportErrorCodes.OracleConnectionFailed,
                $"Oracle connection failed while {operation}.",
                exception.Message);
        }

        return exception is OracleException
            ? new ExportError(
                ExportErrorCodes.OracleSqlFailed,
                $"Oracle SQL failed while {operation}.",
                exception.Message)
            : new ExportError(
                ExportErrorCodes.OracleUnknownError,
                $"Oracle failed while {operation}.",
                exception.Message);
    }

    private static bool IsAuthenticationFailure(Exception exception)
    {
        if (exception is OracleException oracleException && oracleException.Number == 1017)
        {
            return true;
        }

        return exception.Message.Contains("ORA-01017", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsConnectionOperation(string operation)
    {
        return operation.Contains("connection", StringComparison.OrdinalIgnoreCase);
    }
}
