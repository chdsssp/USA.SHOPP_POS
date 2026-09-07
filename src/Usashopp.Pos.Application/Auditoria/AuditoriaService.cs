using Usashopp.Pos.Application.Auditoria.Dtos;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Application.Auditoria;

/// <summary>Escribe y consulta la bitácora de auditoría.</summary>
public class AuditoriaService : IAuditoria
{
    /// <summary>Máximo de registros que devuelve el visor por consulta.</summary>
    private const int MaxResultados = 500;

    private readonly IRepository<RegistroAuditoria> _repo;
    private readonly ICurrentUser _usuario;
    private readonly IDateTime _reloj;
    private readonly IUnitOfWork _uow;

    public AuditoriaService(
        IRepository<RegistroAuditoria> repo,
        ICurrentUser usuario,
        IDateTime reloj,
        IUnitOfWork uow)
    {
        _repo = repo;
        _usuario = usuario;
        _reloj = reloj;
        _uow = uow;
    }

    public async Task RegistrarAsync(
        string accion, string? detalle = null, string? entidad = null,
        Guid? entidadId = null, CancellationToken ct = default)
    {
        try
        {
            await _repo.AgregarAsync(new RegistroAuditoria
            {
                Fecha = _reloj.UtcAhora,
                UsuarioId = _usuario.UsuarioId,
                UsuarioNombre = string.IsNullOrWhiteSpace(_usuario.Nombre) ? "—" : _usuario.Nombre!,
                Accion = accion,
                Detalle = detalle,
                Entidad = entidad,
                EntidadId = entidadId
            }, ct);
            await _uow.GuardarCambiosAsync(ct);
        }
        catch
        {
            // Best-effort: registrar en la bitácora nunca debe romper la operación auditada.
        }
    }

    /// <summary>Registros que cumplen el filtro, del más reciente al más antiguo (tope MaxResultados).</summary>
    public async Task<IReadOnlyList<RegistroAuditoriaDto>> ListarAsync(
        FiltroAuditoriaDto? filtro = null, CancellationToken ct = default)
    {
        filtro ??= new FiltroAuditoriaDto();
        var desde = filtro.Desde;
        var hasta = filtro.Hasta;

        var lista = await _repo.ListarAsync(
            r => (!desde.HasValue || r.Fecha >= desde.Value) &&
                 (!hasta.HasValue || r.Fecha <= hasta.Value), ct);

        IEnumerable<RegistroAuditoria> q = lista.OrderByDescending(r => r.Fecha);

        var texto = filtro.Texto?.Trim();
        if (!string.IsNullOrWhiteSpace(texto))
            q = q.Where(r =>
                r.Accion.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                (r.Detalle?.Contains(texto, StringComparison.OrdinalIgnoreCase) ?? false) ||
                r.UsuarioNombre.Contains(texto, StringComparison.OrdinalIgnoreCase));

        return q.Take(MaxResultados)
            .Select(r => new RegistroAuditoriaDto(r.Fecha, r.UsuarioNombre, r.Accion, r.Detalle))
            .ToList();
    }
}
