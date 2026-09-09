using Microsoft.Extensions.Options;
using Usashopp.Pos.Application.Common.Interfaces;

namespace Usashopp.Pos.Infrastructure.System;

/// <summary>Guarda las imágenes de producto como archivos en la carpeta configurada.</summary>
public class AlmacenImagenes : IAlmacenImagenes
{
    private static readonly string[] ExtensionesValidas = { ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif" };

    private readonly string _carpeta;

    public AlmacenImagenes(IOptions<InfrastructureOptions> opciones) => _carpeta = opciones.Value.CarpetaImagenes;

    public async Task<string> GuardarAsync(string rutaOrigen, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rutaOrigen) || !File.Exists(rutaOrigen))
            throw new FileNotFoundException("No se encontró la imagen a guardar.", rutaOrigen);

        var extension = Path.GetExtension(rutaOrigen).ToLowerInvariant();
        if (!ExtensionesValidas.Contains(extension))
            throw new InvalidOperationException("Formato de imagen no admitido.");

        Directory.CreateDirectory(_carpeta);
        var nombre = $"{Guid.NewGuid():N}{extension}";
        var destino = Path.Combine(_carpeta, nombre);

        using (var origen = File.OpenRead(rutaOrigen))
        using (var salida = File.Create(destino))
            await origen.CopyToAsync(salida, cancellationToken);

        return nombre;
    }

    public string? ObtenerRutaCompleta(string? nombreArchivo)
    {
        if (string.IsNullOrWhiteSpace(nombreArchivo)) return null;
        var ruta = Path.Combine(_carpeta, nombreArchivo);
        return File.Exists(ruta) ? ruta : null;
    }

    public void Eliminar(string? nombreArchivo)
    {
        if (string.IsNullOrWhiteSpace(nombreArchivo)) return;
        try
        {
            var ruta = Path.Combine(_carpeta, nombreArchivo);
            if (File.Exists(ruta)) File.Delete(ruta);
        }
        catch
        {
            // Best-effort: no interrumpir por no poder borrar una imagen.
        }
    }
}
