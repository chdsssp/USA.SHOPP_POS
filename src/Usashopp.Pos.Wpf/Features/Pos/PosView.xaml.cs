using System.Windows.Controls;
using System.Windows.Input;

namespace Usashopp.Pos.Wpf.Features.Pos;

public partial class PosView : UserControl
{
    public PosView()
    {
        InitializeComponent();
        // Enfoca la búsqueda al abrir para operar de inmediato con lector o teclado.
        Loaded += (_, _) => BusquedaBox.Focus();
    }

    /// <summary>Atajos de teclado del POS (F2 cobrar, F3 buscar, F4 descuento, F8 suspender, F9 en espera).</summary>
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not PosViewModel vm) return;

        switch (e.Key)
        {
            case Key.F2:
                if (vm.CobrarCommand.CanExecute(null)) vm.CobrarCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F3:
                BusquedaBox.Focus();
                BusquedaBox.SelectAll();
                e.Handled = true;
                break;
            case Key.F4:
                if (vm.DescuentoGlobalCommand.CanExecute(null)) vm.DescuentoGlobalCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F8:
                if (vm.SuspenderVentaCommand.CanExecute(null)) vm.SuspenderVentaCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F9:
                if (vm.RecuperarVentaCommand.CanExecute(null)) vm.RecuperarVentaCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }
}
