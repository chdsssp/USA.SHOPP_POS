using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Inventario;

public partial class HistorialPrecioWindow : Window
{
    public HistorialPrecioWindow(HistorialPrecioViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
