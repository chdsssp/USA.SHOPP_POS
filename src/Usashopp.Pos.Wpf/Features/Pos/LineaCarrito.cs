using CommunityToolkit.Mvvm.ComponentModel;
using Usashopp.Pos.Domain.Enums;

namespace Usashopp.Pos.Wpf.Features.Pos;

/// <summary>Una línea del carrito de venta (con posible descuento y precio editado).</summary>
public partial class LineaCarrito : ObservableObject
{
    public Guid VarianteId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;

    /// <summary>Precio del catálogo (referencia; no cambia).</summary>
    public decimal PrecioUnitario { get; init; }

    public int StockDisponible { get; init; }

    [ObservableProperty] private int _cantidad = 1;
    /// <summary>Precio con el que se cobra la línea (editable con permiso).</summary>
    [ObservableProperty] private decimal _precioVenta;
    [ObservableProperty] private TipoDescuento? _descuentoTipo;
    [ObservableProperty] private decimal _descuentoValor;

    public decimal Bruto => PrecioVenta * Cantidad;

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
    /// <summary>El precio se editó respecto al del catálogo.</summary>
    public bool PrecioEditado => PrecioVenta != PrecioUnitario;

    partial void OnCantidadChanged(int value)
    {
        if (value < 1) { Cantidad = 1; return; } // no permitir cantidades menores a 1
        NotificarImporte();
    }

    partial void OnPrecioVentaChanged(decimal value)
    {
        if (value < 0) { PrecioVenta = 0; return; }
        OnPropertyChanged(nameof(PrecioEditado));
        NotificarImporte();
    }

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
