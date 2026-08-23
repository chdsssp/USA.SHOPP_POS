using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Pos;

public partial class CorteCajaWindow : Window
{
    public CorteCajaWindow(CorteCajaViewModel viewModel)
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
