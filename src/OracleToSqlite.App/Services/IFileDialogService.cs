namespace OracleToSqlite.App.Services;

public interface IFileDialogService
{
    string? ShowSaveSqliteDialog(string? currentPath);

    string? ShowSelectFolderDialog(string? currentPath);
}
