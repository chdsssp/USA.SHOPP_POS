using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Inventario;

public partial class AjusteStockWindow : Window
{
    public AjusteStockWindow(AjusteStockViewModel viewModel)
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
