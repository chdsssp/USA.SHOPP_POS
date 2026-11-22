using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Clientes;

public partial class SelectorClienteWindow : Window
{
    public SelectorClienteWindow(SelectorClienteViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.Cerrar += resultado =>
        {
            DialogResult = resultado;
            Close();
        };
        Loaded += (_, _) => Busqueda.Focus();
    }
}
