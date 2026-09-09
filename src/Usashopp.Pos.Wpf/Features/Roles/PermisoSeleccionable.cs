using CommunityToolkit.Mvvm.ComponentModel;

namespace Usashopp.Pos.Wpf.Features.Roles;

/// <summary>Permiso con casilla para asignarlo/quitarlo a un rol en el editor.</summary>
public partial class PermisoSeleccionable : ObservableObject
{
    public string Clave { get; init; } = string.Empty;
    public string Etiqueta { get; init; } = string.Empty;

    [ObservableProperty] private bool _asignado;
}
