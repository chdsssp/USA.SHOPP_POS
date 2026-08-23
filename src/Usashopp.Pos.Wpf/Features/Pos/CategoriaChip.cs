using CommunityToolkit.Mvvm.ComponentModel;

namespace Usashopp.Pos.Wpf.Features.Pos;

/// <summary>Chip de categoría del grid del POS (con estado activo para resaltarlo).</summary>
public partial class CategoriaChip : ObservableObject
{
    public Guid? Id { get; }
    public string Nombre { get; }

    [ObservableProperty]
    private bool _activo;

    public CategoriaChip(Guid? id, string nombre)
    {
        Id = id;
        Nombre = nombre;
    }
}
