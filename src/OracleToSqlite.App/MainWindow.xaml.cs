using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using OracleToSqlite.App.ViewModels;

namespace OracleToSqlite.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        viewModel.PropertyChanged += ViewModel_OnPropertyChanged;
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            viewModel.Password = passwordBox.Password;
        }
    }

    private void ViewModel_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.Password) && PasswordInput.Password != viewModel.Password)
        {
            PasswordInput.Password = viewModel.Password;
        }
    }
}
