using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Inventario;

public partial class KardexWindow : Window
{
    public KardexWindow(KardexViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnCerrar(object sender, RoutedEventArgs e) => Close();
}
