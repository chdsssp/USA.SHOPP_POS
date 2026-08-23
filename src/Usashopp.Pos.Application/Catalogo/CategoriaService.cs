using Usashopp.Pos.Application.Catalogo.Dtos;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Application.Catalogo;

public class CategoriaService
{
    private readonly IRepository<Categoria> _categorias;
    private readonly IUnitOfWork _uow;

    public CategoriaService(IRepository<Categoria> categorias, IUnitOfWork uow)
    {
        _categorias = categorias;
        _uow = uow;
    }

    public async Task<IReadOnlyList<CategoriaDto>> ListarAsync(bool incluirInactivas = false, CancellationToken ct = default)
    {
        var lista = incluirInactivas
            ? await _categorias.ListarAsync(null, ct)
            : await _categorias.ListarAsync(c => c.Activo, ct);

        return lista
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaDto(c.Id, c.Nombre, c.Descripcion, c.Activo))
            .ToList();
    }

    public async Task<Result<CategoriaDto>> CrearAsync(string nombre, string? descripcion = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return Result.Falla<CategoriaDto>("El nombre de la categoría es obligatorio.");

        var categoria = new Categoria { Nombre = nombre.Trim(), Descripcion = descripcion };
        await _categorias.AgregarAsync(categoria, ct);
        await _uow.GuardarCambiosAsync(ct);

        return Result.Ok(new CategoriaDto(categoria.Id, categoria.Nombre, categoria.Descripcion, categoria.Activo));
    }

    public async Task<Result> DesactivarAsync(Guid id, CancellationToken ct = default)
    {
        var categoria = await _categorias.ObtenerPorIdAsync(id, ct);
        if (categoria is null)
            return Result.Falla("La categoría no existe.");

        categoria.Activo = false;
        _categorias.Actualizar(categoria);
        await _uow.GuardarCambiosAsync(ct);
        return Result.Ok();
    }
}
