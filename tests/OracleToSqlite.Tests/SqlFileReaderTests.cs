using System.Text;
using FluentAssertions;
using OracleToSqlite.Core.Services;

namespace OracleToSqlite.Tests;

public class SqlFileReaderTests
{
    static SqlFileReaderTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Fact]
    public async Task ReadAsync_ShouldDecodeCp950TraditionalChineseSql()
    {
        var filePath = CreateTempSqlPath();
        var cp950 = Encoding.GetEncoding(950);
        await File.WriteAllBytesAsync(
            filePath,
            cp950.GetBytes("select '住宅_收益' as TYPE_MARK from dual"));
        var reader = new SqlFileReader();

        var result = await reader.ReadAsync(filePath);

        result.SqlText.Should().Contain("住宅_收益");
        result.EncodingName.Should().Be("CP950");
    }

    [Fact]
    public async Task ReadAsync_ShouldDecodeUtf8TraditionalChineseSql()
    {
        var filePath = CreateTempSqlPath();
        await File.WriteAllTextAsync(
            filePath,
            "select 'ADC_企業放款' as RK_NAME from dual",
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        var reader = new SqlFileReader();

        var result = await reader.ReadAsync(filePath);

        result.SqlText.Should().Contain("ADC_企業放款");
        result.EncodingName.Should().Be("UTF-8");
    }

    [Fact]
    public async Task ReadAsync_ShouldDecodeUtf8BomSql()
    {
        var filePath = CreateTempSqlPath();
        await File.WriteAllTextAsync(
            filePath,
            "select '合計' as TYPE_MARK from dual",
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        var reader = new SqlFileReader();

        var result = await reader.ReadAsync(filePath);

        result.SqlText.Should().StartWith("select");
        result.SqlText.Should().Contain("合計");
        result.EncodingName.Should().Be("UTF-8 BOM");
    }

    private static string CreateTempSqlPath()
    {
        return Path.Combine(Path.GetTempPath(), $"oracle-to-sqlite-sql-{Guid.NewGuid():N}.sql");
    }
}
