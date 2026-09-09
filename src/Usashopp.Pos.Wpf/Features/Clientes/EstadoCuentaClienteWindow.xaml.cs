using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Clientes;

public partial class EstadoCuentaClienteWindow : Window
{
    public EstadoCuentaClienteWindow(EstadoCuentaClienteViewModel viewModel)
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
