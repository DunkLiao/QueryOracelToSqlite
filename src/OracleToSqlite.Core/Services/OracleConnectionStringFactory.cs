using Oracle.ManagedDataAccess.Client;
using OracleToSqlite.Core.Models;

namespace OracleToSqlite.Core.Services;

public static class OracleConnectionStringFactory
{
    public static string Create(OracleConnectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var builder = settings.UseFullConnectionString
            ? CreateFromFullConnectionString(settings)
            : CreateFromHostSettings(settings);

        return builder.ConnectionString;
    }

    private static OracleConnectionStringBuilder CreateFromFullConnectionString(
        OracleConnectionSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.FullConnectionString))
        {
            throw new ArgumentException(
                "Full connection string is required when full connection string mode is enabled.",
                nameof(settings));
        }

        var builder = new OracleConnectionStringBuilder(settings.FullConnectionString);

        if (!string.IsNullOrWhiteSpace(settings.Username))
        {
            builder.UserID = settings.Username;
        }

        if (!string.IsNullOrWhiteSpace(settings.Password))
        {
            builder.Password = settings.Password;
        }

        return builder;
    }

    private static OracleConnectionStringBuilder CreateFromHostSettings(
        OracleConnectionSettings settings)
    {
        Require(settings.Host, "Host", nameof(settings));
        Require(settings.ServiceName, "Service name", nameof(settings));
        Require(settings.Username, "Username", nameof(settings));
        Require(settings.Password, "Password", nameof(settings));

        if (settings.Port <= 0)
        {
            throw new ArgumentException("Port must be greater than zero.", nameof(settings));
        }

        var dataSource =
            "(DESCRIPTION=" +
            "(ADDRESS=(PROTOCOL=TCP)" +
            $"(HOST={settings.Host!.Trim()})" +
            $"(PORT={settings.Port}))" +
            "(CONNECT_DATA=" +
            $"(SERVICE_NAME={settings.ServiceName!.Trim()})))";

        return new OracleConnectionStringBuilder
        {
            UserID = settings.Username!.Trim(),
            Password = settings.Password,
            DataSource = dataSource
        };
    }

    private static void Require(string? value, string fieldName, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} is required.", parameterName);
        }
    }
}
