using CommunityToolkit.Mvvm.ComponentModel;
using Usashopp.Pos.Domain.Enums;

namespace Usashopp.Pos.Wpf.Features.Pos;

/// <summary>
/// Una línea del carrito de venta (con posible descuento y precio editado). Los importes
/// (<see cref="DescuentoMonto"/>, <see cref="Importe"/>) NO se calculan aquí: los fija el
/// <see cref="PosViewModel"/> con <c>CalculadoraTotales</c>, la misma lógica que registra la venta.
/// </summary>
public partial class LineaCarrito : ObservableObject
{
    /// <summary>Propiedades capturadas por el cajero: si cambian, hay que recalcular totales.</summary>
    public static readonly IReadOnlySet<string> PropiedadesDeEntrada = new HashSet<string>
    {
        nameof(Cantidad), nameof(PrecioVenta), nameof(DescuentoTipo), nameof(DescuentoValor)
    };

    public Guid VarianteId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string? ImagenRuta { get; init; }

    /// <summary>Precio del catálogo (referencia; no cambia).</summary>
    public decimal PrecioUnitario { get; init; }

    public int StockDisponible { get; init; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Bruto), nameof(ExcedeStock), nameof(AvisoStock))]
    private int _cantidad = 1;

    /// <summary>Precio con el que se cobra la línea (editable con permiso).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Bruto), nameof(PrecioEditado))]
    private decimal _precioVenta;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneDescuento))]
    private TipoDescuento? _descuentoTipo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneDescuento))]
    private decimal _descuentoValor;

    /// <summary>Descuento total de la línea (manual + promoción). Lo fija el ViewModel.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneDescuento))]
    private decimal _descuentoMonto;

    /// <summary>Importe final de la línea. Lo fija el ViewModel.</summary>
    [ObservableProperty] private decimal _importe;

    /// <summary>Nombre de la promoción automática aplicada a la línea, si hay.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TienePromocion))]
    private string? _promocion;

    public decimal Bruto => PrecioVenta * Cantidad;

    public bool TieneDescuento => DescuentoMonto > 0;
    public bool TienePromocion => !string.IsNullOrWhiteSpace(Promocion);

    /// <summary>El precio se editó respecto al del catálogo.</summary>
    public bool PrecioEditado => PrecioVenta != PrecioUnitario;

    /// <summary>La cantidad supera la existencia conocida al agregar la línea.</summary>
    public bool ExcedeStock => Cantidad > StockDisponible;

    public string AvisoStock => StockDisponible <= 0
        ? "Sin existencia"
        : $"Solo hay {StockDisponible}";

    partial void OnCantidadChanged(int value)
    {
        if (value < 1) Cantidad = 1; // no permitir cantidades menores a 1
    }

    partial void OnPrecioVentaChanged(decimal value)
    {
        if (value < 0) PrecioVenta = 0;
    }
}
