using CommunityToolkit.Mvvm.ComponentModel;

namespace Usashopp.Pos.Wpf.Features.Pos;

/// <summary>Una línea del carrito de venta.</summary>
public partial class LineaCarrito : ObservableObject
{
    public Guid VarianteId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public decimal PrecioUnitario { get; init; }
    public int StockDisponible { get; init; }

    [ObservableProperty] private int _cantidad = 1;

    public decimal Importe => PrecioUnitario * Cantidad;

    partial void OnCantidadChanged(int value) => OnPropertyChanged(nameof(Importe));
}
