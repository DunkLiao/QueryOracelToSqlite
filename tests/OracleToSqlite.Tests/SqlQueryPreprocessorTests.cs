using FluentAssertions;
using OracleToSqlite.Core.Models;
using OracleToSqlite.Core.Services;

namespace OracleToSqlite.Tests;

public class SqlQueryPreprocessorTests
{
    [Fact]
    public void Prepare_ShouldKeepColonBindVariablesAndRemoveTrailingSemicolon()
    {
        var parameters = new Dictionary<string, string>
        {
            ["THIS_MONTH_SS_SEQ"] = "202405"
        };

        var result = SqlQueryPreprocessor.Prepare(
            "SELECT * FROM BIS.RPT_CR6010 WHERE SS_SEQ = :THIS_MONTH_SS_SEQ;",
            parameters);

        result.SqlQuery.Should().Be("SELECT * FROM BIS.RPT_CR6010 WHERE SS_SEQ = :THIS_MONTH_SS_SEQ");
        result.Parameters.Should().ContainSingle()
            .Which.Should().Be(new QueryParameter("THIS_MONTH_SS_SEQ", "202405"));
    }

    [Fact]
    public void Prepare_ShouldConvertAmpersandVariablesToBindVariables()
    {
        var parameters = new Dictionary<string, string>
        {
            ["THIS_MONTH_SS_SEQ"] = "202405"
        };

        var result = SqlQueryPreprocessor.Prepare(
            "SELECT * FROM BIS.RPT_CR6010 WHERE SS_SEQ = &THIS_MONTH_SS_SEQ",
            parameters);

        result.SqlQuery.Should().Be("SELECT * FROM BIS.RPT_CR6010 WHERE SS_SEQ = :THIS_MONTH_SS_SEQ");
        result.Parameters.Should().ContainSingle()
            .Which.Should().Be(new QueryParameter("THIS_MONTH_SS_SEQ", "202405"));
    }

    [Fact]
    public void Prepare_ShouldIgnoreSymbolsInsideStringLiterals()
    {
        var parameters = new Dictionary<string, string>
        {
            ["THIS_MONTH_SS_SEQ"] = "202405"
        };

        var result = SqlQueryPreprocessor.Prepare(
            "SELECT ':THIS_MONTH_SS_SEQ; &THIS_MONTH_SS_SEQ' AS TEXT_VALUE, :THIS_MONTH_SS_SEQ AS SS_SEQ FROM DUAL;",
            parameters);

        result.SqlQuery.Should().Be("SELECT ':THIS_MONTH_SS_SEQ; &THIS_MONTH_SS_SEQ' AS TEXT_VALUE, :THIS_MONTH_SS_SEQ AS SS_SEQ FROM DUAL");
        result.Parameters.Should().ContainSingle()
            .Which.Should().Be(new QueryParameter("THIS_MONTH_SS_SEQ", "202405"));
    }

    [Fact]
    public void Prepare_ShouldThrowValidationException_WhenRequiredParameterIsMissing()
    {
        var action = () => SqlQueryPreprocessor.Prepare(
            "SELECT * FROM BIS.RPT_CR6010 WHERE SS_SEQ = :THIS_MONTH_SS_SEQ",
            new Dictionary<string, string>());

        action.Should().Throw<ArgumentException>()
            .WithMessage("*THIS_MONTH_SS_SEQ*");
    }

    [Fact]
    public void ParseParameterText_ShouldNormalizePrefixesAndRejectDuplicateNames()
    {
        var action = () => SqlQueryPreprocessor.ParseParameterText(
            """
            :THIS_MONTH_SS_SEQ=202405
            &THIS_MONTH_SS_SEQ=202406
            """);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*THIS_MONTH_SS_SEQ*");
    }

    [Fact]
    public void ParseParameterText_ShouldParseNameValueLines()
    {
        var result = SqlQueryPreprocessor.ParseParameterText(
            """
            :THIS_MONTH_SS_SEQ = 202405

            REPORT_CODE=CR6010
            """);

        result.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["THIS_MONTH_SS_SEQ"] = "202405",
            ["REPORT_CODE"] = "CR6010"
        });
    }
}
