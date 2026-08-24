using System.Windows.Controls;

namespace Usashopp.Pos.Wpf.Features.Pos;

public partial class PosView : UserControl
{
    public PosView()
    {
        InitializeComponent();
        // Enfoca la búsqueda al abrir para operar de inmediato con lector o teclado.
        Loaded += (_, _) => BusquedaBox.Focus();
    }
}
