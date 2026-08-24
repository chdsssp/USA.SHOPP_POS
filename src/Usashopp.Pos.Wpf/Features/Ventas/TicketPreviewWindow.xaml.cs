using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Ventas;

public partial class TicketPreviewWindow : Window
{
    public TicketPreviewWindow(TicketPreviewViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnCerrar(object sender, RoutedEventArgs e) => Close();
}
