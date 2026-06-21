using CommunityToolkit.Mvvm.ComponentModel;

namespace OracleToSqlite.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public string Title { get; } = "Oracle To SQLite";
}
