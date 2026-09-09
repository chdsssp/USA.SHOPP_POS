using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Compras;

public partial class PagoCompraWindow : Window
{
    public PagoCompraWindow(PagoCompraViewModel viewModel)
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
