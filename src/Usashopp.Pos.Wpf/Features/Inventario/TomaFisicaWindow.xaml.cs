using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Inventario;

public partial class TomaFisicaWindow : Window
{
    public TomaFisicaWindow(TomaFisicaViewModel viewModel)
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
