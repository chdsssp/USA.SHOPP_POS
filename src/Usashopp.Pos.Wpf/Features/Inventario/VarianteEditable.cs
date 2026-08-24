using CommunityToolkit.Mvvm.ComponentModel;

namespace Usashopp.Pos.Wpf.Features.Inventario;

/// <summary>Fila editable de variante en el alta de producto.</summary>
public partial class VarianteEditable : ObservableObject
{
    /// <summary>Id de la variante si ya existe (null = nueva).</summary>
    public Guid? Id { get; set; }

    [ObservableProperty] private string _sku = string.Empty;
    [ObservableProperty] private string? _codigoBarras;
    [ObservableProperty] private string? _talla;
    [ObservableProperty] private string? _color;
    [ObservableProperty] private decimal _precio;
    [ObservableProperty] private decimal _costo;
    [ObservableProperty] private int _stockInicial;
    [ObservableProperty] private int _stockMinimo;
}
