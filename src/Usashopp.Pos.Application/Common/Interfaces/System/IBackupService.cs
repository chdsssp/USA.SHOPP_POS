namespace Usashopp.Pos.Application.Common.Interfaces.System;

/// <summary>Respaldo y restauración de la base de datos local.</summary>
public interface IBackupService
{
    /// <summary>Crea un respaldo con marca de tiempo y devuelve la ruta generada.</summary>
    Task<string> CrearRespaldoAsync(CancellationToken cancellationToken = default);

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
