using CommunityToolkit.Mvvm.ComponentModel;

namespace Usashopp.Pos.Wpf.Features.Compras;

/// <summary>Fila del diálogo de devolución a proveedor.</summary>
public partial class DevolucionProveedorEditable : ObservableObject
{
    public Guid VarianteId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public int Recibido { get; init; }

    [ObservableProperty] private int _aDevolver;

    partial void OnADevolverChanged(int value)
    {
        if (value < 0) ADevolver = 0;
        else if (value > Recibido) ADevolver = Recibido;
    }
}
