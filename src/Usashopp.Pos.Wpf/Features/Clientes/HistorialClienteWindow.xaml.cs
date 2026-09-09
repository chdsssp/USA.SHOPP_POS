using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Clientes;

public partial class HistorialClienteWindow : Window
{
    public HistorialClienteWindow(HistorialClienteViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
