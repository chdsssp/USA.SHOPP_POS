using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Pos;

public partial class SelectorProductoWindow : Window
{
    public SelectorProductoWindow(SelectorProductoViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.Cerrar += resultado =>
        {
            DialogResult = resultado;
            Close();
        };
        Loaded += (_, _) => BuscadorBox.Focus();
    }
}
