using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Application.Proveedores.Dtos;
using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Application.Proveedores;

public class ProveedorService
{
    private readonly IRepository<Proveedor> _proveedores;
    private readonly IUnitOfWork _uow;

    public ProveedorService(IRepository<Proveedor> proveedores, IUnitOfWork uow)
    {
        _proveedores = proveedores;
        _uow = uow;
    }

    public async Task<IReadOnlyList<ProveedorDto>> ListarAsync(string? texto = null, CancellationToken ct = default)
    {
        IReadOnlyList<Proveedor> lista;
        if (string.IsNullOrWhiteSpace(texto))
        {
            lista = await _proveedores.ListarAsync(p => p.Activo, ct);
        }
        else
        {
            var t = texto.Trim();
            lista = await _proveedores.ListarAsync(
                p => p.Activo && (p.Nombre.Contains(t) || (p.Contacto != null && p.Contacto.Contains(t))), ct);
        }
        return lista.OrderBy(p => p.Nombre).Select(Map).ToList();
    }

    public async Task<Result> CrearAsync(ProveedorDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return Result.Falla("El nombre del proveedor es obligatorio.");

        await _proveedores.AgregarAsync(new Proveedor
        {
            Nombre = dto.Nombre.Trim(),
            Contacto = dto.Contacto,
            Telefono = dto.Telefono,
            Email = dto.Email
        }, ct);
        await _uow.GuardarCambiosAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> ActualizarAsync(ProveedorDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return Result.Falla("El nombre del proveedor es obligatorio.");

        var proveedor = await _proveedores.ObtenerPorIdAsync(dto.Id, ct);
        if (proveedor is null) return Result.Falla("El proveedor no existe.");

        proveedor.Nombre = dto.Nombre.Trim();
        proveedor.Contacto = dto.Contacto;
        proveedor.Telefono = dto.Telefono;
        proveedor.Email = dto.Email;
        _proveedores.Actualizar(proveedor);
        await _uow.GuardarCambiosAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> DesactivarAsync(Guid id, CancellationToken ct = default)
    {
        var proveedor = await _proveedores.ObtenerPorIdAsync(id, ct);
        if (proveedor is null) return Result.Falla("El proveedor no existe.");
        proveedor.Activo = false;
        _proveedores.Actualizar(proveedor);
        await _uow.GuardarCambiosAsync(ct);
        return Result.Ok();
    }

    private static ProveedorDto Map(Proveedor p) => new(p.Id, p.Nombre, p.Contacto, p.Telefono, p.Email, p.Activo);
}
