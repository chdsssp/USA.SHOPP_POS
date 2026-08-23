using CommunityToolkit.Mvvm.ComponentModel;

namespace Usashopp.Pos.Wpf.Features.Shell;

/// <summary>Un elemento de la barra de navegación lateral.</summary>
public partial class MenuItemViewModel : ObservableObject
{
    public string Clave { get; }
    public string Titulo { get; }

    [ObservableProperty]
    private bool _activo;

    public MenuItemViewModel(string clave, string titulo)
    {
        Clave = clave;
        Titulo = titulo;
    }
}
