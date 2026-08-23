using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Inventario;

public partial class ProductoEditorWindow : Window
{
    public ProductoEditorWindow(ProductoEditorViewModel viewModel)
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
