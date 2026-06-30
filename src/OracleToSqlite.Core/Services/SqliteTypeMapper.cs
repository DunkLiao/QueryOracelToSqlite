using OracleToSqlite.Core.Models;

namespace OracleToSqlite.Core.Services;

internal static class SqliteTypeMapper
{
    public static string Map(OracleColumnSchema column)
    {
        var oracleType = Normalize(column.OracleDataTypeName);
        var mappedOracleType = MapOracleType(column, oracleType);

        if (mappedOracleType is not null)
        {
            return mappedOracleType;
        }

        return MapProviderType(column.ProviderTypeName) ?? "TEXT";
    }

    private static string? MapOracleType(OracleColumnSchema column, string oracleType)
    {
        if (string.IsNullOrWhiteSpace(oracleType) || oracleType == "UNKNOWN")
        {
            return null;
        }

        if (oracleType == "NUMBER")
        {
            if (column.Scale == 0)
            {
                return "INTEGER";
            }

            if (column.Scale > 0)
            {
                return "REAL";
            }

            return "NUMERIC";
        }

        if (oracleType is "BLOB" or "RAW" or "LONG RAW")
        {
            return "BLOB";
        }

        if (oracleType == "DATE" || oracleType.StartsWith("TIMESTAMP", StringComparison.Ordinal))
        {
            return "TEXT";
        }

        if (oracleType.Contains("CHAR", StringComparison.Ordinal) || oracleType is "CLOB" or "NCLOB" or "LONG")
        {
            return "TEXT";
        }

        if (oracleType is "BINARY_FLOAT" or "BINARY_DOUBLE" or "FLOAT")
        {
            return "REAL";
        }

        return null;
    }

    private static string? MapProviderType(string providerTypeName)
    {
        return Normalize(providerTypeName) switch
        {
            "BYTE" or "SBYTE" or "INT16" or "UINT16" or "INT32" or "UINT32" or "INT64" or "UINT64" => "INTEGER",
            "SINGLE" or "DOUBLE" => "REAL",
            "DECIMAL" => "NUMERIC",
            "BYTE[]" => "BLOB",
            "DATETIME" or "DATETIMEOFFSET" or "STRING" or "CHAR" => "TEXT",
            _ => null
        };
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToUpperInvariant();
    }
}
