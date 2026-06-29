using System.Text;
using OracleToSqlite.Core.Models;

namespace OracleToSqlite.Core.Services;

public static class SqlQueryPreprocessor
{
    public static IReadOnlyDictionary<string, string> ParseParameterText(string? parameterText)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(parameterText))
        {
            return parameters;
        }

        var lines = parameterText
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                throw new ArgumentException($"SQL parameter line {index + 1} must use name=value format.");
            }

            var name = NormalizeParameterName(line[..separatorIndex].Trim());
            var value = line[(separatorIndex + 1)..].Trim();

            if (name.Length == 0)
            {
                throw new ArgumentException($"SQL parameter line {index + 1} must include a parameter name.");
            }

            if (!parameters.TryAdd(name, value))
            {
                throw new ArgumentException($"SQL parameter '{name}' is defined more than once.");
            }
        }

        return parameters;
    }

    public static PreparedSqlQuery Prepare(
        string sqlQuery,
        IReadOnlyDictionary<string, string>? parameters)
    {
        if (string.IsNullOrWhiteSpace(sqlQuery))
        {
            throw new ArgumentException("SQL query is required.", nameof(sqlQuery));
        }

        var normalizedParameters = NormalizeParameters(parameters);
        var trimmedSql = RemoveTrailingSemicolon(sqlQuery.Trim());
        var detectedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var preparedSql = NormalizeVariables(trimmedSql, detectedNames);
        var missingNames = detectedNames
            .Where(name => !normalizedParameters.ContainsKey(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (missingNames.Length > 0)
        {
            throw new ArgumentException($"Missing SQL parameter value(s): {string.Join(", ", missingNames)}.");
        }

        var queryParameters = detectedNames
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name => new QueryParameter(name, normalizedParameters[name]))
            .ToArray();

        return new PreparedSqlQuery(preparedSql, queryParameters);
    }

    private static Dictionary<string, string> NormalizeParameters(IReadOnlyDictionary<string, string>? parameters)
    {
        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (parameters is null)
        {
            return normalized;
        }

        foreach (var parameter in parameters)
        {
            var name = NormalizeParameterName(parameter.Key);
            if (name.Length == 0)
            {
                throw new ArgumentException("SQL parameter name cannot be empty.");
            }

            if (!normalized.TryAdd(name, parameter.Value))
            {
                throw new ArgumentException($"SQL parameter '{name}' is defined more than once.");
            }
        }

        return normalized;
    }

    private static string NormalizeParameterName(string name)
    {
        return name.Trim().TrimStart(':', '&');
    }

    private static string RemoveTrailingSemicolon(string sqlQuery)
    {
        return sqlQuery.EndsWith(';')
            ? sqlQuery[..^1].TrimEnd()
            : sqlQuery;
    }

    private static string NormalizeVariables(string sqlQuery, HashSet<string> detectedNames)
    {
        var builder = new StringBuilder(sqlQuery.Length);
        var inString = false;

        for (var index = 0; index < sqlQuery.Length; index++)
        {
            var current = sqlQuery[index];

            if (current == '\'')
            {
                builder.Append(current);

                if (inString && index + 1 < sqlQuery.Length && sqlQuery[index + 1] == '\'')
                {
                    builder.Append(sqlQuery[index + 1]);
                    index++;
                    continue;
                }

                inString = !inString;
                continue;
            }

            if (!inString && (current == ':' || current == '&') && TryReadName(sqlQuery, index + 1, out var name, out var endIndex))
            {
                detectedNames.Add(name);
                builder.Append(':');
                builder.Append(name);
                index = endIndex - 1;
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }

    private static bool TryReadName(string sqlQuery, int startIndex, out string name, out int endIndex)
    {
        endIndex = startIndex;
        name = string.Empty;

        if (startIndex >= sqlQuery.Length || !IsNameStart(sqlQuery[startIndex]))
        {
            return false;
        }

        while (endIndex < sqlQuery.Length && IsNamePart(sqlQuery[endIndex]))
        {
            endIndex++;
        }

        name = sqlQuery[startIndex..endIndex];
        return true;
    }

    private static bool IsNameStart(char value)
    {
        return char.IsLetter(value) || value == '_';
    }

    private static bool IsNamePart(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
    }
}
