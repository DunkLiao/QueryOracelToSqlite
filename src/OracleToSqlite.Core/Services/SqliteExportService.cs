using System.Globalization;
using Microsoft.Data.Sqlite;
using OracleToSqlite.Core.Models;

namespace OracleToSqlite.Core.Services;

public sealed class SqliteExportService : ISqliteExportService
{
    public async Task<long> ExportAsync(
        ExportJobSettings settings,
        OracleQueryResult result,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(result);

        var directory = Path.GetDirectoryName(settings.SqliteFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var connection = new SqliteConnection($"Data Source={settings.SqliteFilePath}");
        await connection.OpenAsync(cancellationToken);

        await using var transaction = connection.BeginTransaction();

        if (settings.OverwriteExisting)
        {
            await using var dropCommand = connection.CreateCommand();
            dropCommand.Transaction = transaction;
            dropCommand.CommandText = $"DROP TABLE IF EXISTS {QuoteIdentifier(settings.TargetTableName)}";
            await dropCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var createCommand = connection.CreateCommand())
        {
            createCommand.Transaction = transaction;
            createCommand.CommandText = CreateTableSql(settings.TargetTableName, result.Columns);
            await createCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        var rowsWritten = 0L;
        foreach (var row in result.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var insertCommand = connection.CreateCommand();
            insertCommand.Transaction = transaction;
            insertCommand.CommandText = CreateInsertSql(settings.TargetTableName, result.Columns);

            foreach (var column in result.Columns)
            {
                var parameter = insertCommand.CreateParameter();
                parameter.ParameterName = $"$p{column.Ordinal}";
                parameter.Value = ConvertValue(row.TryGetValue(column.ColumnName, out var value) ? value : null);
                insertCommand.Parameters.Add(parameter);
            }

            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            rowsWritten++;
            progress?.Report(new ExportProgress(ExportStatus.Running, rowsWritten, "Writing rows to SQLite."));
        }

        transaction.Commit();
        return rowsWritten;
    }

    private static string CreateTableSql(string tableName, IReadOnlyList<OracleColumnSchema> columns)
    {
        if (columns.Count == 0)
        {
            throw new ArgumentException("Query result must contain at least one column.", nameof(columns));
        }

        var columnDefinitions = columns
            .OrderBy(column => column.Ordinal)
            .Select(column => $"{QuoteIdentifier(column.ColumnName)} {MapToSqliteType(column)}");

        return $"CREATE TABLE {QuoteIdentifier(tableName)} ({string.Join(", ", columnDefinitions)})";
    }

    private static string CreateInsertSql(string tableName, IReadOnlyList<OracleColumnSchema> columns)
    {
        var orderedColumns = columns.OrderBy(column => column.Ordinal).ToArray();
        var columnNames = orderedColumns.Select(column => QuoteIdentifier(column.ColumnName));
        var parameterNames = orderedColumns.Select(column => $"$p{column.Ordinal}");

        return $"INSERT INTO {QuoteIdentifier(tableName)} ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameterNames)})";
    }

    private static string QuoteIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("SQLite identifier is required.", nameof(identifier));
        }

        return $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static string MapToSqliteType(OracleColumnSchema column)
    {
        var oracleType = column.OracleDataTypeName.Trim().ToUpperInvariant();

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

        return "TEXT";
    }

    private static object ConvertValue(object? value)
    {
        return value switch
        {
            null => DBNull.Value,
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            decimal decimalValue => decimal.ToDouble(decimalValue),
            _ => value
        };
    }
}
