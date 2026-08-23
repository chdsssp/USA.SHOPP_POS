namespace Usashopp.Pos.Application.Common.Interfaces.System;

/// <summary>Respaldo y restauración de la base de datos local.</summary>
public interface IBackupService
{
    /// <summary>Crea un respaldo con marca de tiempo y devuelve la ruta generada.</summary>
    Task<string> CrearRespaldoAsync(CancellationToken cancellationToken = default);

    /// <summary>Restaura la base de datos desde un archivo de respaldo.</summary>
    Task RestaurarAsync(string rutaRespaldo, CancellationToken cancellationToken = default);
}
