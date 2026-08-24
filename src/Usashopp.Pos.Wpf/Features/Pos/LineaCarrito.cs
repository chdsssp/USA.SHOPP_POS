using CommunityToolkit.Mvvm.ComponentModel;
using Usashopp.Pos.Domain.Enums;

namespace Usashopp.Pos.Wpf.Features.Pos;

/// <summary>Una línea del carrito de venta (con posible descuento de línea).</summary>
public partial class LineaCarrito : ObservableObject
{
    public Guid VarianteId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public decimal PrecioUnitario { get; init; }
    public int StockDisponible { get; init; }

    [ObservableProperty] private int _cantidad = 1;
    [ObservableProperty] private TipoDescuento? _descuentoTipo;
    [ObservableProperty] private decimal _descuentoValor;

    public decimal Bruto => PrecioUnitario * Cantidad;

    public decimal DescuentoMonto
    {
        get
        {
            if (DescuentoTipo is null || DescuentoValor <= 0) return 0m;
            var monto = DescuentoTipo == TipoDescuento.Porcentaje
                ? Bruto * (DescuentoValor / 100m)
                : DescuentoValor;
            return Math.Round(Math.Min(monto, Bruto), 2);
        }
    }

    public decimal Importe => Bruto - DescuentoMonto;

    public bool TieneDescuento => DescuentoTipo is not null && DescuentoValor > 0;

    partial void OnCantidadChanged(int value) => NotificarImporte();
    partial void OnDescuentoTipoChanged(TipoDescuento? value) => NotificarImporte();
    partial void OnDescuentoValorChanged(decimal value) => NotificarImporte();

    private void NotificarImporte()
    {
        OnPropertyChanged(nameof(Bruto));
        OnPropertyChanged(nameof(DescuentoMonto));
        OnPropertyChanged(nameof(Importe));
        OnPropertyChanged(nameof(TieneDescuento));
    }
}
