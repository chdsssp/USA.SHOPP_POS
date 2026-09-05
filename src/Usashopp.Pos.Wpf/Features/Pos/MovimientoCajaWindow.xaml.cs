using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Pos;

public partial class MovimientoCajaWindow : Window
{
    public MovimientoCajaWindow(MovimientoCajaViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnCerrar(object sender, RoutedEventArgs e) => Close();
}
