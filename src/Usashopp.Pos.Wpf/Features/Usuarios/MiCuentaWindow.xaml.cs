using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Usuarios;

public partial class MiCuentaWindow : Window
{
    private readonly MiCuentaViewModel _viewModel;

    public MiCuentaWindow(MiCuentaViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.Cerrar += resultado =>
        {
            DialogResult = resultado;
            Close();
        };
        Loaded += (_, _) => Actual.Focus();
    }

    private async void OnGuardar(object sender, RoutedEventArgs e) =>
        await _viewModel.GuardarAsync(Actual.Password, Nueva.Password, Confirmar.Password);

    private void OnCancelar(object sender, RoutedEventArgs e) => _viewModel.Cancelar();
}
