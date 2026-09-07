namespace Usashopp.Pos.Application.Common.Interfaces;

/// <summary>
/// Registra acciones relevantes en la bitácora de auditoría. Best-effort: un fallo al
/// registrar nunca debe interrumpir la operación de negocio que se está auditando.
/// </summary>
public interface IAuditoria
{
    Task RegistrarAsync(
        string accion,
        string? detalle = null,
        string? entidad = null,
        Guid? entidadId = null,
        CancellationToken ct = default);
}
