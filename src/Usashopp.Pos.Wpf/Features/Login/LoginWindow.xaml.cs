using System.Windows;
using System.Windows.Input;

namespace Usashopp.Pos.Wpf.Features.Login;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.LoginExitoso += () => { DialogResult = true; };
        Loaded += (_, _) => Password.Focus();
        // Escape cierra la aplicación desde la pantalla de login.
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) System.Windows.Application.Current.Shutdown(); };
    }

    private async void OnLogin(object sender, RoutedEventArgs e) =>
        await _viewModel.IntentarAsync(Password.Password);

    private async void OnPasswordKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            await _viewModel.IntentarAsync(Password.Password);
    }
}
