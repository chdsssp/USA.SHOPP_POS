using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Pos;

public partial class CambioTallaWindow : Window
{
    public CambioTallaWindow(CambioTallaViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.Cerrar += resultado =>
        {
            DialogResult = resultado;
            Close();
        };
        viewModel.EnfocarBusqueda = () => BusquedaBox.Focus();
        Loaded += (_, _) => FolioBox.Focus();
    }

    /// <summary>Precarga un folio (p. ej. escaneado desde el POS) y lo busca.</summary>
    public void Precargar(string? folio)
    {
        if (string.IsNullOrWhiteSpace(folio) || DataContext is not CambioTallaViewModel vm) return;
        vm.Folio = folio;
        vm.BuscarVentaCommand.Execute(null);
    }
}
