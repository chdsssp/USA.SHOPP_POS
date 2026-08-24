using CommunityToolkit.Mvvm.ComponentModel;

namespace Usashopp.Pos.Wpf.Features.Shell;

/// <summary>Un elemento de la barra de navegación lateral.</summary>
public partial class MenuItemViewModel : ObservableObject
{
    public string Clave { get; }
    public string Titulo { get; }

    /// <summary>Geometría del icono (mini-lenguaje de rutas, coordenadas en 24x24).</summary>
    public string IconData { get; }

    /// <summary>Permiso requerido para ver el ítem (null = siempre visible).</summary>
    public string? Permiso { get; }

    [ObservableProperty]
    private bool _activo;

    public MenuItemViewModel(string clave, string titulo, string iconData, string? permiso = null)
    {
        Clave = clave;
        Titulo = titulo;
        IconData = iconData;
        Permiso = permiso;
    }
}
