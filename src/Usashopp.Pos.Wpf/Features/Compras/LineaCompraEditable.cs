namespace Usashopp.Pos.Wpf.Features.Compras;

/// <summary>Línea de una compra en el editor (valores fijados al agregar).</summary>
public class LineaCompraEditable
{
    public Guid VarianteId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public int Cantidad { get; init; }
    public decimal Costo { get; init; }
    public decimal Importe => Cantidad * Costo;
}
