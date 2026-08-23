using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Apartados;

public partial class AbonoWindow : Window
{
    public AbonoWindow(AbonoViewModel viewModel)
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
