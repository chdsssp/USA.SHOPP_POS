namespace Usashopp.Pos.Application.Common.Interfaces.System;

/// <summary>Por qué se creó un respaldo (se refleja en el nombre del archivo y en la lista).</summary>
public enum OrigenRespaldo
{
    Manual = 0,
    Automatico = 1,
    Corte = 2,
    PreMigracion = 3,
    PreActualizacion = 4
}

/// <summary>Un respaldo existente en la carpeta de respaldos.</summary>
public record RespaldoInfo(
    string Ruta,
    string Nombre,
    DateTime Fecha,
    long TamanoBytes,
    OrigenRespaldo Origen,
    bool? Verificado)
{
    public string TamanoTexto => TamanoBytes >= 1024 * 1024
        ? $"{TamanoBytes / 1024d / 1024d:N1} MB"
        : $"{TamanoBytes / 1024d:N0} KB";

    public string OrigenTexto => Origen switch
    {
        OrigenRespaldo.Automatico => "Automático",
        OrigenRespaldo.Corte => "Corte de caja",
        OrigenRespaldo.PreMigracion => "Antes de actualizar BD",
        OrigenRespaldo.PreActualizacion => "Antes de actualizar app",
        _ => "Manual"
    };

    public string VerificadoTexto => Verificado switch { true => "✓ Íntegro", false => "✗ Dañado", _ => "—" };
}

/// <summary>Estado del respaldo externo (USB/disco) para avisar si está atrasado.</summary>
public record EstadoRespaldoExterno(bool Configurado, bool Disponible, DateTime? Ultimo)
{
    /// <summary>Días desde el último respaldo externo (null si nunca).</summary>
    public int? DiasSinRespaldo(DateTime ahora) => Ultimo is { } u ? (int)(ahora - u).TotalDays : null;
}

/// <summary>Preferencias de respaldo que dependen del equipo (las provee la UI).</summary>
public interface IPreferenciasRespaldo
{
    /// <summary>Carpeta externa (USB/disco) a la que se copia cada respaldo; null = desactivado.</summary>
    string? CarpetaRespaldoExterno { get; }
}

/// <summary>Respaldo y restauración de la base de datos local.</summary>
public interface IBackupService
{
    /// <summary>Crea un respaldo manual con marca de tiempo y devuelve la ruta generada.</summary>
    Task<string> CrearRespaldoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea un respaldo consistente (API de respaldo en línea de SQLite, aunque la app esté
    /// escribiendo), lo verifica con <c>PRAGMA integrity_check</c>, lo copia a la carpeta
    /// externa/nube si están configuradas y aplica la política de retención.
    /// </summary>
    Task<string> CrearRespaldoAsync(OrigenRespaldo origen, string? etiqueta = null, CancellationToken cancellationToken = default);

    /// <summary>Respaldos existentes, del más reciente al más antiguo.</summary>
    IReadOnlyList<RespaldoInfo> ListarRespaldos();

    /// <summary>Comprueba que un archivo de respaldo se abre y está íntegro.</summary>
    bool VerificarRespaldo(string ruta);

    /// <summary>Si la carpeta externa está configurada, conectada y cuándo fue el último respaldo ahí.</summary>
    EstadoRespaldoExterno ObtenerEstadoExterno();

    /// <summary>Restaura la base de datos desde un archivo de respaldo.</summary>
    Task RestaurarAsync(string rutaRespaldo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca un respaldo para restaurarlo en el próximo arranque (antes de abrir la base).
    /// No se puede restaurar en caliente porque el archivo .db está en uso.
    /// </summary>
    void ProgramarRestauracion(string rutaRespaldo);

    /// <summary>
    /// Si hay una restauración pendiente, la aplica (copia el respaldo sobre la base) y
    /// devuelve true. Debe llamarse al arranque, antes de cualquier consulta a la base.
    /// </summary>
    bool AplicarRestauracionPendiente();
}
