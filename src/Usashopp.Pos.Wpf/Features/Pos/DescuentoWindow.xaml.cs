using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Pos;

public partial class DescuentoWindow : Window
{
    public DescuentoWindow(DescuentoViewModel viewModel)
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
