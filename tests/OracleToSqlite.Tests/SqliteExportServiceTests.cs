using FluentAssertions;
using Microsoft.Data.Sqlite;
using OracleToSqlite.Core.Models;
using OracleToSqlite.Core.Services;

namespace OracleToSqlite.Tests;

public class SqliteExportServiceTests
{
    [Fact]
    public async Task ExportAsync_ShouldCreateQuotedTableAndWriteTypedValues()
    {
        var databasePath = CreateTempDatabasePath();
        var settings = CreateSettings(databasePath, "Report Table");
        var result = new OracleQueryResult(
            new[]
            {
                new OracleColumnSchema(0, "CUSTOMER ID", "NUMBER", "Decimal", 10, 0, false),
                new OracleColumnSchema(1, "AMOUNT", "NUMBER", "Decimal", 12, 2, true),
                new OracleColumnSchema(2, "CREATED_AT", "DATE", "DateTime", null, null, true),
                new OracleColumnSchema(3, "NOTE", "VARCHAR2", "String", null, null, true),
                new OracleColumnSchema(4, "PAYLOAD", "BLOB", "Byte[]", null, null, true)
            },
            new[]
            {
                new Dictionary<string, object?>
                {
                    ["CUSTOMER ID"] = 42,
                    ["AMOUNT"] = 10.5m,
                    ["CREATED_AT"] = new DateTime(2026, 6, 22, 9, 30, 15),
                    ["NOTE"] = "hello",
                    ["PAYLOAD"] = new byte[] { 1, 2, 3 }
                },
                new Dictionary<string, object?>
                {
                    ["CUSTOMER ID"] = 43,
                    ["AMOUNT"] = null,
                    ["CREATED_AT"] = null,
                    ["NOTE"] = "world",
                    ["PAYLOAD"] = null
                }
            });

        var service = new SqliteExportService();

        var rowsWritten = await service.ExportAsync(settings, result);

        rowsWritten.Should().Be(2);
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();

        await using (var schemaCommand = connection.CreateCommand())
        {
            schemaCommand.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'Report Table'";
            var createSql = (string?)await schemaCommand.ExecuteScalarAsync();
            createSql.Should().Contain("\"CUSTOMER ID\" INTEGER");
            createSql.Should().Contain("\"AMOUNT\" REAL");
            createSql.Should().Contain("\"CREATED_AT\" TEXT");
            createSql.Should().Contain("\"PAYLOAD\" BLOB");
        }

        await using var queryCommand = connection.CreateCommand();
        queryCommand.CommandText = "SELECT \"CUSTOMER ID\", \"AMOUNT\", \"CREATED_AT\", \"NOTE\", \"PAYLOAD\" FROM \"Report Table\" ORDER BY \"CUSTOMER ID\"";
        await using var reader = await queryCommand.ExecuteReaderAsync();

        (await reader.ReadAsync()).Should().BeTrue();
        reader.GetInt64(0).Should().Be(42);
        reader.GetDouble(1).Should().Be(10.5);
        reader.GetString(2).Should().Be("2026-06-22T09:30:15.0000000");
        reader.GetString(3).Should().Be("hello");
        reader.GetFieldValue<byte[]>(4).Should().Equal(1, 2, 3);

        (await reader.ReadAsync()).Should().BeTrue();
        reader.GetInt64(0).Should().Be(43);
        reader.IsDBNull(1).Should().BeTrue();
        reader.IsDBNull(2).Should().BeTrue();
        reader.GetString(3).Should().Be("world");
        reader.IsDBNull(4).Should().BeTrue();
    }

    [Fact]
    public async Task ExportAsync_ShouldOverwriteExistingTable_WhenOverwriteExistingIsTrue()
    {
        var databasePath = CreateTempDatabasePath();
        await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE \"Customers\" (\"OldColumn\" TEXT); INSERT INTO \"Customers\" VALUES ('stale');";
            await command.ExecuteNonQueryAsync();
        }

        var settings = CreateSettings(databasePath, "Customers", overwriteExisting: true);
        var result = new OracleQueryResult(
            new[]
            {
                new OracleColumnSchema(0, "ID", "NUMBER", "Decimal", 10, 0, false)
            },
            new[]
            {
                new Dictionary<string, object?> { ["ID"] = 1 }
            });

        var service = new SqliteExportService();

        await service.ExportAsync(settings, result);

        await using var verifyConnection = new SqliteConnection($"Data Source={databasePath}");
        await verifyConnection.OpenAsync();
        await using var verifyCommand = verifyConnection.CreateCommand();
        verifyCommand.CommandText = "SELECT COUNT(*) FROM \"Customers\" WHERE \"ID\" = 1";
        var count = (long)(await verifyCommand.ExecuteScalarAsync())!;

        count.Should().Be(1);
    }

    private static ExportJobSettings CreateSettings(
        string databasePath,
        string tableName,
        bool overwriteExisting = true)
    {
        return new ExportJobSettings
        {
            Connection = new OracleConnectionSettings
            {
                Host = "db.example.local",
                ServiceName = "ORCLPDB1",
                Username = "user",
                Password = "password"
            },
            SqlQuery = "select * from customers",
            SqliteFilePath = databasePath,
            TargetTableName = tableName,
            OverwriteExisting = overwriteExisting
        };
    }

    private static string CreateTempDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), $"oracle-to-sqlite-{Guid.NewGuid():N}.db");
    }
}
