using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Compras;

public partial class RecepcionCompraWindow : Window
{
    public RecepcionCompraWindow(RecepcionCompraViewModel viewModel)
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
