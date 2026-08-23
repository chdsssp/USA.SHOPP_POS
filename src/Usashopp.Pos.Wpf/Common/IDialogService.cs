using Usashopp.Pos.Application.Inventario.Dtos;
using Usashopp.Pos.Wpf.Features.Pos;

namespace Usashopp.Pos.Wpf.Common;

/// <summary>Abre ventanas modales desde los ViewModels (sin acoplarlos a WPF).</summary>
public interface IDialogService
{
    /// <summary>Editor de alta de producto. Devuelve true si se guardó.</summary>
    bool MostrarEditorProducto();

    /// <summary>Diálogo de ajuste de stock de una variante. Devuelve true si se ajustó.</summary>
    bool MostrarAjusteStock(VarianteInventarioDto variante);

    /// <summary>Diálogo de cobro. Devuelve los pagos y el cambio, o null si se canceló.</summary>
    CobroResultado? MostrarCobro(decimal total);

    /// <summary>Diálogo de apertura de caja. Devuelve el fondo inicial, o null si se canceló.</summary>
    decimal? MostrarAbrirCaja();

    /// <summary>Mensaje simple de información/error.</summary>
    void Mensaje(string texto, string titulo = "USASHOPP POS");
}
