using CommunityToolkit.Mvvm.ComponentModel;

namespace Usashopp.Pos.Wpf.Features.Pos;

/// <summary>Renglón del conteo de efectivo por denominación (billete o moneda) en el corte.</summary>
public partial class DenominacionConteo : ObservableObject
{
    /// <summary>Valor nominal de la pieza (p. ej. 500, 0.50).</summary>
    public decimal Valor { get; init; }

    /// <summary>Etiqueta a mostrar (p. ej. "$500", "50¢").</summary>
    public string Etiqueta { get; init; } = string.Empty;

    [ObservableProperty] private int _cantidad;

    /// <summary>Importe de este renglón: valor × cantidad.</summary>
    public decimal Importe => Valor * Cantidad;

    partial void OnCantidadChanged(int value)
    {
        if (value < 0) { Cantidad = 0; return; }
        OnPropertyChanged(nameof(Importe));
    }
}
