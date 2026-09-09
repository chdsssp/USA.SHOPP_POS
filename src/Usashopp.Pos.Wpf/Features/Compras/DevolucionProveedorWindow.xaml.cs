using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Compras;

public partial class DevolucionProveedorWindow : Window
{
    public DevolucionProveedorWindow(DevolucionProveedorViewModel viewModel)
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
