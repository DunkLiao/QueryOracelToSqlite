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
        ValidateColumns(result.Columns);

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
            .Select(column => $"{QuoteIdentifier(column.ColumnName)} {SqliteTypeMapper.Map(column)}");

        return $"CREATE TABLE {QuoteIdentifier(tableName)} ({string.Join(", ", columnDefinitions)})";
    }

    private static void ValidateColumns(IReadOnlyList<OracleColumnSchema> columns)
    {
        if (columns.Count == 0)
        {
            throw new ArgumentException("Query result must contain at least one column.", nameof(columns));
        }

        var duplicateNames = columns
            .GroupBy(column => column.ColumnName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.First().ColumnName)
            .ToArray();

        if (duplicateNames.Length > 0)
        {
            throw new ArgumentException(
                $"Duplicate column names are not supported: {string.Join(", ", duplicateNames)}.",
                nameof(columns));
        }
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
