using System.Collections.ObjectModel;
using Usashopp.Pos.Domain.Enums;

namespace Usashopp.Pos.Wpf.Features.Pos;

/// <summary>Una línea de un carrito suspendido (copia de <see cref="LineaCarrito"/>).</summary>
public class LineaEnEspera
{
    public Guid VarianteId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public decimal PrecioUnitario { get; init; }
    public int StockDisponible { get; init; }
    public int Cantidad { get; init; }
    public TipoDescuento? DescuentoTipo { get; init; }
    public decimal DescuentoValor { get; init; }
}

/// <summary>Un carrito puesto en espera para atender a otro cliente y retomarlo después.</summary>
public class VentaEnEspera
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime Fecha { get; } = DateTime.Now;
    public string Etiqueta { get; init; } = string.Empty;

    public Guid? ClienteId { get; init; }
    public string? ClienteNombre { get; init; }
    public TipoDescuento? DescuentoGlobalTipo { get; init; }
    public decimal DescuentoGlobalValor { get; init; }
    public string? Notas { get; init; }
    public decimal Total { get; init; }

    public IReadOnlyList<LineaEnEspera> Lineas { get; init; } = new List<LineaEnEspera>();

    public int Articulos => Lineas.Sum(l => l.Cantidad);
    public string HoraTexto => Fecha.ToString("HH:mm");
}

/// <summary>
/// Almacén en memoria de ventas suspendidas. Es singleton para que las ventas en espera
/// sobrevivan mientras la app está abierta, aunque se navegue a otro módulo y se regrese.
/// </summary>
public class VentasEnEsperaStore
{
    public ObservableCollection<VentaEnEspera> Items { get; } = new();
}
