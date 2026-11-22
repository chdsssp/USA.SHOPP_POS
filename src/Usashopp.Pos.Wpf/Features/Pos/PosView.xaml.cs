using System.Windows.Controls;
using System.Windows.Input;
using Usashopp.Pos.Wpf.Common;

namespace Usashopp.Pos.Wpf.Features.Pos;

public partial class PosView : UserControl
{
    public PosView()
    {
        InitializeComponent();
        // Enfoca la búsqueda al abrir para operar de inmediato con lector o teclado.
        Loaded += (_, _) => BusquedaBox.Focus();
        // Al salir del POS (otro módulo, cambio de cajero) el carrito se guarda como borrador al momento.
        Unloaded += async (_, _) => { if (DataContext is PosViewModel vm) await vm.GuardarBorradorAhoraAsync(); };

        // Lector de códigos en toda la vista (no solo con el foco en la búsqueda).
        _ = new LectorCodigoBarras(this, BusquedaBox, async codigo =>
        {
            if (DataContext is PosViewModel vm) await vm.ProcesarCodigoEscaneadoAsync(codigo);
        });

        DataContextChanged += (_, _) =>
        {
            if (DataContext is PosViewModel vm)
                vm.PedirFocoBusqueda = () => { BusquedaBox.Focus(); BusquedaBox.SelectAll(); };
        };
    }

    /// <summary>Atajos de teclado del POS (F2 cobrar, F3 buscar, F4 descuento, F8 suspender, F9 en espera).</summary>
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not PosViewModel vm) return;

        switch (e.Key)
        {
            case Key.F1:
                vm.MostrarAtajosCommand.Execute(null);
                e.Handled = true;
                break;
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
