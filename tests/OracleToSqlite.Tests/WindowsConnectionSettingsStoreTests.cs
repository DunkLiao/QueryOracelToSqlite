using FluentAssertions;
using OracleToSqlite.App.Services;

namespace OracleToSqlite.Tests;

public class WindowsConnectionSettingsStoreTests
{
    [Fact]
    public void SaveLoadAndClear_ShouldPersistConnectionSettingsWithoutPlainTextPassword()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"oracle-to-sqlite-settings-{Guid.NewGuid():N}.json");
        var store = new WindowsConnectionSettingsStore(filePath);
        var settings = new StoredConnectionSettings(
            "db.example.local",
            "1521",
            "ORCLPDB1",
            "report_user",
            "secret-password");

        store.Save(settings);

        File.ReadAllText(filePath).Should().NotContain("secret-password");
        store.Load().Should().Be(settings);

        store.Clear();

        File.Exists(filePath).Should().BeFalse();
    }
}
