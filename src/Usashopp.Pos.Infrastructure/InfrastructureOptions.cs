namespace Usashopp.Pos.Infrastructure;

/// <summary>Opciones de infraestructura leídas de appsettings.json.</summary>
public class InfrastructureOptions
{
    /// <summary>Cadena de conexión a la base de datos SQLite.</summary>
    public string ConnectionString { get; set; } = "Data Source=pos.db";

    /// <summary>Carpeta donde se guardan los respaldos.</summary>
    public string CarpetaRespaldos { get; set; } = "backups";

    /// <summary>Carpeta donde se guardan las imágenes de producto.</summary>
    public string CarpetaImagenes { get; set; } = "imagenes";

    /// <summary>Carpeta adicional (nube sincronizada) para copiar el respaldo; opcional.</summary>
    public string? CarpetaNube { get; set; }

    /// <summary>Número de respaldos a conservar.</summary>
    public int RetenerUltimos { get; set; } = 30;
}
