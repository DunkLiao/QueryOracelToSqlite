using FluentAssertions;
using OracleToSqlite.Core.Models;
using OracleToSqlite.Core.Services;
using System.Data;

namespace OracleToSqlite.Tests;

public class OracleQueryServiceTests
{
    [Fact]
    public void Create_ShouldBuildHostPortServiceNameConnectionString_WhenHostModeIsUsed()
    {
        var settings = new OracleConnectionSettings
        {
            Host = "db.example.local",
            Port = 1522,
            ServiceName = "ORCLPDB1",
            Username = "report_user",
            Password = "secret"
        };

        var connectionString = OracleConnectionStringFactory.Create(settings);

        connectionString.Should().ContainEquivalentOf("User Id=report_user");
        connectionString.Should().ContainEquivalentOf("Password=secret");
        connectionString.Should().Contain("HOST=db.example.local");
        connectionString.Should().Contain("PORT=1522");
        connectionString.Should().Contain("SERVICE_NAME=ORCLPDB1");
    }

    [Fact]
    public void Create_ShouldPreserveFullConnectionStringAndOverrideCredentials_WhenFullConnectionStringModeIsUsed()
    {
        var settings = new OracleConnectionSettings
        {
            UseFullConnectionString = true,
            FullConnectionString = "Data Source=SampleDb;User Id=old_user;Password=old_password;",
            Username = "new_user",
            Password = "new_password"
        };

        var connectionString = OracleConnectionStringFactory.Create(settings);

        connectionString.Should().ContainEquivalentOf("Data Source=SampleDb");
        connectionString.Should().ContainEquivalentOf("User Id=new_user");
        connectionString.Should().ContainEquivalentOf("Password=new_password");
    }

    [Fact]
    public void Create_ShouldRejectMissingHostSettings_WhenHostModeRequiredFieldsAreEmpty()
    {
        var settings = new OracleConnectionSettings
        {
            Host = "",
            ServiceName = "ORCLPDB1",
            Username = "report_user",
            Password = "secret"
        };

        var action = () => OracleConnectionStringFactory.Create(settings);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*Host*");
    }

    [Fact]
    public void Create_ShouldRejectMissingFullConnectionString_WhenFullConnectionStringModeIsUsed()
    {
        var settings = new OracleConnectionSettings
        {
            UseFullConnectionString = true,
            FullConnectionString = " "
        };

        var action = () => OracleConnectionStringFactory.Create(settings);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*Full connection string*");
    }

    [Fact]
    public void FromSchemaTable_ShouldMapOracleColumnMetadata()
    {
        using var schemaTable = CreateSchemaTable();
        schemaTable.Rows.Add(
            "CUSTOMER_ID",
            0,
            typeof(decimal),
            "NUMBER",
            10,
            0,
            false);
        schemaTable.Rows.Add(
            "CREATED_AT",
            1,
            typeof(DateTime),
            "DATE",
            DBNull.Value,
            DBNull.Value,
            true);

        var columns = OracleColumnSchemaReader.FromSchemaTable(schemaTable);

        columns.Should().BeEquivalentTo(
            new[]
            {
                new OracleColumnSchema(0, "CUSTOMER_ID", "NUMBER", "Decimal", 10, 0, false),
                new OracleColumnSchema(1, "CREATED_AT", "DATE", "DateTime", null, null, true)
            },
            options => options.WithStrictOrdering());
    }

    [Fact]
    public void ToExportError_ShouldClassifyOra01017AsAuthenticationFailure()
    {
        var exception = new InvalidOperationException("ORA-01017: invalid username/password; logon denied");

        var error = OracleErrorMapper.ToExportError(exception, "testing Oracle connection");

        error.Code.Should().Be(ExportErrorCodes.OracleAuthenticationFailed);
        error.Message.Should().Be("Oracle authentication failed while testing Oracle connection.");
        error.Detail.Should().Contain("ORA-01017");
    }

    [Fact]
    public void ToExportError_ShouldClassifyConnectionTestFailuresAsConnectionFailures()
    {
        var exception = new InvalidOperationException("Unable to reach Oracle listener.");

        var error = OracleErrorMapper.ToExportError(exception, "testing Oracle connection");

        error.Code.Should().Be(ExportErrorCodes.OracleConnectionFailed);
        error.Message.Should().Be("Oracle connection failed while testing Oracle connection.");
        error.Detail.Should().Contain("Oracle listener");
    }

    private static DataTable CreateSchemaTable()
    {
        var table = new DataTable();
        table.Columns.Add("ColumnName", typeof(string));
        table.Columns.Add("ColumnOrdinal", typeof(int));
        table.Columns.Add("DataType", typeof(Type));
        table.Columns.Add("DataTypeName", typeof(string));
        table.Columns.Add("NumericPrecision", typeof(int));
        table.Columns.Add("NumericScale", typeof(int));
        table.Columns.Add("AllowDBNull", typeof(bool));
        return table;
    }
}
