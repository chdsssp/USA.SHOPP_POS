using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Clientes;

public partial class ClienteEditorWindow : Window
{
    public ClienteEditorWindow(ClienteEditorViewModel viewModel)
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
