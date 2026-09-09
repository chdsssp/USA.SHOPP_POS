using Usashopp.Pos.Application.Common;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Application.Usuarios.Dtos;
using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Application.Usuarios;

/// <summary>Gestión de roles personalizables y sus permisos.</summary>
public class RolService
{
    /// <summary>Rol de sistema protegido (superusuario; no se edita ni elimina desde la UI).</summary>
    private const string RolSistema = "Administrador";

    private readonly IRolRepository _roles;
    private readonly IUsuarioRepository _usuarios;
    private readonly IRepository<Permiso> _permisos;
    private readonly IUnitOfWork _uow;
    private readonly IAuditoria _auditoria;

    public RolService(
        IRolRepository roles,
        IUsuarioRepository usuarios,
        IRepository<Permiso> permisos,
        IUnitOfWork uow,
        IAuditoria auditoria)
    {
        _roles = roles;
        _usuarios = usuarios;
        _permisos = permisos;
        _uow = uow;
        _auditoria = auditoria;
    }

    public async Task<IReadOnlyList<RolDetalleDto>> ListarAsync(CancellationToken ct = default)
    {
        var roles = await _roles.ListarConPermisosAsync(ct);
        var usuarios = await _usuarios.ListarAsync(null, ct);
        var conteo = usuarios.GroupBy(u => u.RolId).ToDictionary(g => g.Key, g => g.Count());

        return roles
            .OrderBy(r => r.Nombre)
            .Select(r => new RolDetalleDto(
                r.Id, r.Nombre, conteo.GetValueOrDefault(r.Id, 0), r.Permisos.Count, EsSistema(r.Nombre)))
            .ToList();
    }

    /// <summary>Catálogo de permisos con su etiqueta y si el rol indicado los tiene (todos falsos si es nuevo).</summary>
    public async Task<IReadOnlyList<PermisoOpcionDto>> ObtenerPermisosAsync(Guid? rolId, CancellationToken ct = default)
    {
        var asignados = new HashSet<string>();
        if (rolId is { } id)
        {
            var rol = (await _roles.ListarConPermisosAsync(ct)).FirstOrDefault(r => r.Id == id);
            if (rol is not null)
                asignados = rol.Permisos.Select(p => p.Clave).ToHashSet();
        }

        return Permisos.Todos
            .Select(c => new PermisoOpcionDto(c, Permisos.Etiqueta(c), asignados.Contains(c)))
            .ToList();
    }

    public async Task<Result> CrearAsync(GuardarRolDto dto, CancellationToken ct = default)
    {
        var error = Validar(dto);
        if (error is not null) return Result.Falla(error);

        var nombre = dto.Nombre.Trim();
        var existentes = await _roles.ListarConPermisosAsync(ct);
        if (existentes.Any(r => r.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase)))
            return Result.Falla($"Ya existe un rol «{nombre}».");

        var permisos = await ResolverPermisosAsync(dto.Permisos, ct);
        var rol = new Rol { Nombre = nombre };
        foreach (var p in permisos) rol.Permisos.Add(p);

        await _roles.AgregarAsync(rol, ct);
        await _uow.GuardarCambiosAsync(ct);
        await _auditoria.RegistrarAsync("Alta de rol", $"Rol «{nombre}» ({permisos.Count} permisos)", "Rol", rol.Id);
        return Result.Ok();
    }

    public async Task<Result> ActualizarAsync(GuardarRolDto dto, CancellationToken ct = default)
    {
        var error = Validar(dto);
        if (error is not null) return Result.Falla(error);

        var roles = await _roles.ListarConPermisosAsync(ct);
        var rol = roles.FirstOrDefault(r => r.Id == dto.Id);
        if (rol is null) return Result.Falla("El rol no existe.");
        if (EsSistema(rol.Nombre)) return Result.Falla("El rol Administrador no se puede modificar.");

        var nombre = dto.Nombre.Trim();
        if (roles.Any(r => r.Id != dto.Id && r.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase)))
            return Result.Falla($"Ya existe un rol «{nombre}».");

        var permisos = await ResolverPermisosAsync(dto.Permisos, ct);
        rol.Nombre = nombre;
        rol.Permisos.Clear();
        foreach (var p in permisos) rol.Permisos.Add(p);

        _roles.Actualizar(rol);
        await _uow.GuardarCambiosAsync(ct);
        await _auditoria.RegistrarAsync("Edición de rol", $"Rol «{nombre}» ({permisos.Count} permisos)", "Rol", rol.Id);
        return Result.Ok();
    }

    public async Task<Result> EliminarAsync(Guid id, CancellationToken ct = default)
    {
        var rol = (await _roles.ListarConPermisosAsync(ct)).FirstOrDefault(r => r.Id == id);
        if (rol is null) return Result.Falla("El rol no existe.");
        if (EsSistema(rol.Nombre)) return Result.Falla("El rol Administrador no se puede eliminar.");

        var conUsuarios = (await _usuarios.ListarAsync(u => u.RolId == id, ct)).Count;
        if (conUsuarios > 0)
            return Result.Falla($"No se puede eliminar: {conUsuarios} usuario(s) tienen este rol.");

        _roles.Eliminar(rol);
        await _uow.GuardarCambiosAsync(ct);
        await _auditoria.RegistrarAsync("Baja de rol", $"Rol «{rol.Nombre}»", "Rol", rol.Id);
        return Result.Ok();
    }

    private async Task<List<Permiso>> ResolverPermisosAsync(IReadOnlyList<string> claves, CancellationToken ct)
    {
        var set = claves.ToHashSet();
        var todos = await _permisos.ListarAsync(null, ct);
        return todos.Where(p => set.Contains(p.Clave)).ToList();
    }

    private static bool EsSistema(string nombre) => nombre.Equals(RolSistema, StringComparison.OrdinalIgnoreCase);

    private static string? Validar(GuardarRolDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre)) return "El nombre del rol es obligatorio.";
        if (dto.Nombre.Trim().Length < 3) return "El nombre del rol debe tener al menos 3 caracteres.";
        return null;
    }
}
