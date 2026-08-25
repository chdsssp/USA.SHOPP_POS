using System.Windows;
using System.Windows.Input;

namespace Usashopp.Pos.Wpf.Features.Shell;

public partial class MainWindow : Window
{
    private bool _pantallaCompleta;
    private WindowStyle _estiloPrevio;
    private WindowState _estadoPrevio;
    private ResizeMode _resizePrevio;

    public MainWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        // F11 alterna pantalla completa (modo kiosco); Esc sale de ella.
        if (e.Key == Key.F11)
        {
            AlternarPantallaCompleta();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _pantallaCompleta)
        {
            AlternarPantallaCompleta();
            e.Handled = true;
        }
        base.OnPreviewKeyDown(e);
    }

    private void AlternarPantallaCompleta()
    {
        if (!_pantallaCompleta)
        {
            _estiloPrevio = WindowStyle;
            _estadoPrevio = WindowState;
            _resizePrevio = ResizeMode;

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Normal;   // necesario para re-maximizar sin borde
            WindowState = WindowState.Maximized;
            _pantallaCompleta = true;
        }
        else
        {
            WindowStyle = _estiloPrevio;
            ResizeMode = _resizePrevio;
            WindowState = _estadoPrevio;
            _pantallaCompleta = false;
        }
    }
}
