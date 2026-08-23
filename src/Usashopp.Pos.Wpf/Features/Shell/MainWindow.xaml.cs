using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Shell;

public partial class MainWindow : Window
{
    public MainWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
