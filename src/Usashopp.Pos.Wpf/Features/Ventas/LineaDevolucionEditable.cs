using CommunityToolkit.Mvvm.ComponentModel;

namespace Usashopp.Pos.Wpf.Features.Ventas;

/// <summary>Fila editable del diálogo de devolución (cantidad a devolver por variante).</summary>
public partial class LineaDevolucionEditable : ObservableObject
{
    public Guid VarianteId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public decimal PrecioUnitario { get; init; }
    public int Vendida { get; init; }
    public int Devuelta { get; init; }
    public int Disponible { get; init; }

    [ObservableProperty] private int _aDevolver;

    partial void OnADevolverChanged(int value)
    {
        // Acota el valor al rango válido [0, Disponible].
        if (value < 0) ADevolver = 0;
        else if (value > Disponible) ADevolver = Disponible;
    }
}
