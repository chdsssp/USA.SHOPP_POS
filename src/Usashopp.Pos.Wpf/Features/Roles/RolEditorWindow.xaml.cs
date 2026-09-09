using System.Windows;

namespace Usashopp.Pos.Wpf.Features.Roles;

public partial class RolEditorWindow : Window
{
    public RolEditorWindow(RolEditorViewModel viewModel)
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
