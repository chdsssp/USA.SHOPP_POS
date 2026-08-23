namespace Usashopp.Pos.Wpf.Features.Apartados;

public class LineaApartadoEditable
{
    public Guid VarianteId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public int Cantidad { get; init; }
    public decimal Precio { get; init; }
    public decimal Importe => Cantidad * Precio;
}
