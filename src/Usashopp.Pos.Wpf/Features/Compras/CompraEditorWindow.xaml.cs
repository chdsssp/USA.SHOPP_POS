using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Compras;

public partial class CompraEditorWindow : Window
{
    public CompraEditorWindow(CompraEditorViewModel viewModel)
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
