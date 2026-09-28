namespace Usashopp.Pos.Wpf.Features.Etiquetas;

/// <summary>Datos que se dibujan en una etiqueta 4×6" de producto.</summary>
public class EtiquetaDatos
{
    /// <summary>Ruta absoluta del logo de la tienda (o null para mostrar el nombre en texto).</summary>
    public string? LogoAbsoluto { get; init; }
    public string NombreNegocio { get; init; } = "";
    public bool TieneLogo => !string.IsNullOrWhiteSpace(LogoAbsoluto);

    public string Talla { get; init; } = "";
    public string Nombre { get; init; } = "";
    public string Marca { get; init; } = "";
    public string Color { get; init; } = "";

    /// <summary>Código con el que se genera el código de barras.</summary>
    public string Codigo { get; init; } = "";
    public bool TieneCodigo => !string.IsNullOrWhiteSpace(Codigo);
}
