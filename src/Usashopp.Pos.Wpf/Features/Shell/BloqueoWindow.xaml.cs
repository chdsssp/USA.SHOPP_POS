using System.ComponentModel;
using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Shell;

public partial class BloqueoWindow : Window
{
    private readonly BloqueoViewModel _vm;
    private bool _desbloqueado;

    public BloqueoWindow(BloqueoViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = viewModel;
        viewModel.Desbloqueado += () => { _desbloqueado = true; DialogResult = true; };
        Loaded += (_, _) => Pad.Focus();
    }

    /// <summary>No se puede cerrar (Alt+F4) sin desbloquear.</summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_desbloqueado) e.Cancel = true;
        base.OnClosing(e);
    }

    private async void OnPin(object? sender, string pin) => await _vm.IntentarAsync(Contrasena.Password);
    private async void OnDesbloquear(object sender, RoutedEventArgs e) => await _vm.IntentarAsync(Contrasena.Password);
}
