using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Pos;

public partial class CobroWindow : Window
{
    public CobroWindow(CobroViewModel viewModel)
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
