using System.Windows;
using System.Windows.Input;

namespace Usashopp.Pos.Wpf.Features.Usuarios;

public partial class AutorizacionSupervisorWindow : Window
{
    private readonly AutorizacionSupervisorViewModel _viewModel;

    public AutorizacionSupervisorWindow(AutorizacionSupervisorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.Cerrar += resultado =>
        {
            DialogResult = resultado;
            Close();
        };
        Loaded += (_, _) => Login.Focus();
    }

    private async void OnAutorizar(object sender, RoutedEventArgs e) =>
        await _viewModel.AutorizarAsync(Contrasena.Password);

    private async void OnContrasenaKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            await _viewModel.AutorizarAsync(Contrasena.Password);
    }

    private void OnCancelar(object sender, RoutedEventArgs e) => _viewModel.Cancelar();
}
