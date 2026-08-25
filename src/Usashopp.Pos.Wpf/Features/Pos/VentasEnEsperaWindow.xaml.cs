using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Pos;

public partial class VentasEnEsperaWindow : Window
{
    public VentasEnEsperaWindow(VentasEnEsperaViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.Cerrar += resultado =>
        {
            DialogResult = resultado;
            Close();
        };
    }
}
