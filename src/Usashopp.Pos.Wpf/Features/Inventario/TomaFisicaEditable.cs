using CommunityToolkit.Mvvm.ComponentModel;

namespace Usashopp.Pos.Wpf.Features.Inventario;

/// <summary>Renglón de conteo físico: stock del sistema vs. lo contado.</summary>
public partial class TomaFisicaEditable : ObservableObject
{
    public Guid VarianteId { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public int StockSistema { get; init; }

    [ObservableProperty] private int _conteo;

    /// <summary>Diferencia entre lo contado y el sistema (para resaltar).</summary>
    public int Diferencia => Conteo - StockSistema;

    partial void OnConteoChanged(int value)
    {
        if (value < 0) { Conteo = 0; return; }
        OnPropertyChanged(nameof(Diferencia));
    }
}
