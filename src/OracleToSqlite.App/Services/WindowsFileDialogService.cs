using System.IO;
using Microsoft.Win32;

namespace OracleToSqlite.App.Services;

public sealed class WindowsFileDialogService : IFileDialogService
{
    public string? ShowSaveSqliteDialog(string? currentPath)
    {
        var dialog = new SaveFileDialog
        {
            AddExtension = true,
            DefaultExt = ".db",
            FileName = string.IsNullOrWhiteSpace(currentPath) ? "export.db" : Path.GetFileName(currentPath),
            Filter = "SQLite database (*.db)|*.db|All files (*.*)|*.*",
            OverwritePrompt = false,
            Title = "Select SQLite output file"
        };

        var directory = string.IsNullOrWhiteSpace(currentPath)
            ? null
            : Path.GetDirectoryName(currentPath);

        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
        {
            dialog.InitialDirectory = directory;
        }

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
