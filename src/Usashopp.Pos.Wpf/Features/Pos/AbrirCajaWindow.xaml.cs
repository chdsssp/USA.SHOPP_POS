using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Pos;

public partial class AbrirCajaWindow : Window
{
    public AbrirCajaWindow(AbrirCajaViewModel viewModel)
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
