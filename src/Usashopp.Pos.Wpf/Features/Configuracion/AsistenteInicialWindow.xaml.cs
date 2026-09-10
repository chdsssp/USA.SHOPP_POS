using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Configuracion;

public partial class AsistenteInicialWindow : Window
{
    private readonly AsistenteInicialViewModel _viewModel;

    public AsistenteInicialWindow(AsistenteInicialViewModel viewModel)
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
        await _viewModel.GuardarAsync(Actual.Password, Nueva.Password, Confirmar.Password);

    private void OnOmitir(object sender, RoutedEventArgs e) => _viewModel.Omitir();
}
