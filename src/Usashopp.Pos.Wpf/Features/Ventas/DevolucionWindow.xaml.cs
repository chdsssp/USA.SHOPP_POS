using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Ventas;

public partial class DevolucionWindow : Window
{
    public DevolucionWindow(DevolucionViewModel viewModel)
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
