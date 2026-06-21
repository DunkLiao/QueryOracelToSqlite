using System.Data;
using OracleToSqlite.Core.Models;

namespace OracleToSqlite.Core.Services;

public static class OracleColumnSchemaReader
{
    public static IReadOnlyList<OracleColumnSchema> FromSchemaTable(DataTable? schemaTable)
    {
        if (schemaTable is null)
        {
            return Array.Empty<OracleColumnSchema>();
        }

        var columns = new List<OracleColumnSchema>(schemaTable.Rows.Count);

        foreach (DataRow row in schemaTable.Rows)
        {
            columns.Add(new OracleColumnSchema(
                ReadInt(row, "ColumnOrdinal") ?? columns.Count,
                ReadString(row, "ColumnName", $"Column{columns.Count + 1}"),
                ReadString(row, "DataTypeName", "UNKNOWN"),
                ReadProviderTypeName(row),
                ReadInt(row, "NumericPrecision"),
                ReadInt(row, "NumericScale"),
                ReadBool(row, "AllowDBNull") ?? true));
        }

        return columns
            .OrderBy(column => column.Ordinal)
            .ToArray();
    }

    private static string ReadProviderTypeName(DataRow row)
    {
        if (!row.Table.Columns.Contains("DataType") || row["DataType"] is DBNull)
        {
            return "Object";
        }

        return row["DataType"] is Type type
            ? type.Name
            : row["DataType"]?.ToString() ?? "Object";
    }

    private static string ReadString(DataRow row, string columnName, string fallback)
    {
        if (!row.Table.Columns.Contains(columnName) || row[columnName] is DBNull)
        {
            return fallback;
        }

        var value = row[columnName]?.ToString();
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static int? ReadInt(DataRow row, string columnName)
    {
        if (!row.Table.Columns.Contains(columnName) || row[columnName] is DBNull)
        {
            return null;
        }

        return Convert.ToInt32(row[columnName]);
    }

    private static bool? ReadBool(DataRow row, string columnName)
    {
        if (!row.Table.Columns.Contains(columnName) || row[columnName] is DBNull)
        {
            return null;
        }

        return Convert.ToBoolean(row[columnName]);
    }
}
