namespace OracleToSqlite.App.Services;

public interface IConnectionSettingsStore
{
    StoredConnectionSettings? Load();

    void Save(StoredConnectionSettings settings);

    void Clear();
}
