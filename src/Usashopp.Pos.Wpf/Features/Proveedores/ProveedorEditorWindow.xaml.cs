using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Proveedores;

public partial class ProveedorEditorWindow : Window
{
    public ProveedorEditorWindow(ProveedorEditorViewModel viewModel)
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
