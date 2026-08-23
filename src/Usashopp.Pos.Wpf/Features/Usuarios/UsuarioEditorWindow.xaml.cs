using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Usuarios;

public partial class UsuarioEditorWindow : Window
{
    private readonly UsuarioEditorViewModel _viewModel;

    public UsuarioEditorWindow(UsuarioEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.Cerrar += resultado =>
        {
            DialogResult = resultado;
            Close();
        };
    }

    private async void OnGuardar(object sender, RoutedEventArgs e) =>
        await _viewModel.GuardarAsync(Password.Password);

    private void OnCancelar(object sender, RoutedEventArgs e) => _viewModel.Cancelar();
}
