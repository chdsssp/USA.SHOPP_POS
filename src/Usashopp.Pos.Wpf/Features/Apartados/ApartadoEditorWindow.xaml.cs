using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Apartados;

public partial class ApartadoEditorWindow : Window
{
    public ApartadoEditorWindow(ApartadoEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.Cerrar += resultado =>
        {
            DialogResult = resultado;
            Close();
        };
    }
}
