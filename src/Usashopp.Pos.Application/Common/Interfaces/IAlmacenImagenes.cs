namespace Usashopp.Pos.Application.Common.Interfaces;

/// <summary>
/// Almacena las imágenes de producto como archivos en disco. La entidad solo guarda el
/// nombre del archivo; la ruta completa la resuelve la implementación (infraestructura).
/// </summary>
public interface IAlmacenImagenes
{
    /// <summary>Copia el archivo indicado al almacén y devuelve el nombre con el que quedó guardado.</summary>
    Task<string> GuardarAsync(string rutaOrigen, CancellationToken cancellationToken = default);

    /// <summary>Ruta absoluta del archivo guardado (o null si no hay imagen o no existe).</summary>
    string? ObtenerRutaCompleta(string? nombreArchivo);

    /// <summary>Elimina el archivo del almacén (best-effort).</summary>
    void Eliminar(string? nombreArchivo);
}
