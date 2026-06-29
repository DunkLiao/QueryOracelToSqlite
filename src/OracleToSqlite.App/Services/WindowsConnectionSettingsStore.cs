using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OracleToSqlite.App.Services;

public sealed class WindowsConnectionSettingsStore : IConnectionSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string filePath;

    public WindowsConnectionSettingsStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OracleToSqlite",
            "connection-settings.json"))
    {
    }

    public WindowsConnectionSettingsStore(string filePath)
    {
        this.filePath = filePath;
    }

    public StoredConnectionSettings? Load()
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var stored = JsonSerializer.Deserialize<StoredConnectionSettingsFile>(json);
            if (stored is null)
            {
                return null;
            }

            return new StoredConnectionSettings(
                stored.Host ?? string.Empty,
                string.IsNullOrWhiteSpace(stored.Port) ? "1521" : stored.Port,
                stored.ServiceName ?? string.Empty,
                stored.Username ?? string.Empty,
                Unprotect(stored.ProtectedPassword));
        }
        catch (Exception exception) when (exception is IOException or JsonException or CryptographicException or FormatException)
        {
            return null;
        }
    }

    public void Save(StoredConnectionSettings settings)
    {
        var directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var stored = new StoredConnectionSettingsFile
        {
            Host = settings.Host,
            Port = settings.Port,
            ServiceName = settings.ServiceName,
            Username = settings.Username,
            ProtectedPassword = Protect(settings.Password)
        };

        File.WriteAllText(filePath, JsonSerializer.Serialize(stored, JsonOptions));
    }

    public void Clear()
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private static string Protect(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(value);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    private static string Unprotect(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var protectedBytes = Convert.FromBase64String(value);
        var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }

    private sealed class StoredConnectionSettingsFile
    {
        public string? Host { get; init; }

        public string? Port { get; init; }

        public string? ServiceName { get; init; }

        public string? Username { get; init; }

        public string? ProtectedPassword { get; init; }
    }
}
