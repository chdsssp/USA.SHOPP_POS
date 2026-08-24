using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Categorias;

public partial class CategoriaEditorWindow : Window
{
    public CategoriaEditorWindow(CategoriaEditorViewModel viewModel)
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
