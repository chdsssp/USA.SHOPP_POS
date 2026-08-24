using Usashopp.Pos.Application.Catalogo.Dtos;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Application.Catalogo;

public class CategoriaService
{
    private readonly IRepository<Categoria> _categorias;
    private readonly IRepository<Producto> _productos;
    private readonly IUnitOfWork _uow;

    public CategoriaService(IRepository<Categoria> categorias, IRepository<Producto> productos, IUnitOfWork uow)
    {
        _categorias = categorias;
        _productos = productos;
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

        if (await ExisteNombreAsync(nombre, null, ct))
            return Result.Falla<CategoriaDto>($"Ya existe una categoría llamada «{nombre.Trim()}».");

        var categoria = new Categoria { Nombre = nombre.Trim(), Descripcion = descripcion };
        await _categorias.AgregarAsync(categoria, ct);
        await _uow.GuardarCambiosAsync(ct);

        return Result.Ok(new CategoriaDto(categoria.Id, categoria.Nombre, categoria.Descripcion, categoria.Activo));
    }

    public async Task<Result> ActualizarAsync(Guid id, string nombre, string? descripcion = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return Result.Falla("El nombre de la categoría es obligatorio.");

        var categoria = await _categorias.ObtenerPorIdAsync(id, ct);
        if (categoria is null)
            return Result.Falla("La categoría no existe.");

        if (await ExisteNombreAsync(nombre, id, ct))
            return Result.Falla($"Ya existe una categoría llamada «{nombre.Trim()}».");

        categoria.Nombre = nombre.Trim();
        categoria.Descripcion = descripcion;
        _categorias.Actualizar(categoria);
        await _uow.GuardarCambiosAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> DesactivarAsync(Guid id, CancellationToken ct = default)
    {
        var categoria = await _categorias.ObtenerPorIdAsync(id, ct);
        if (categoria is null)
            return Result.Falla("La categoría no existe.");

        // No se puede eliminar una categoría que aún tiene productos asignados.
        var productos = await _productos.ListarAsync(p => p.CategoriaId == id, ct);
        if (productos.Count > 0)
            return Result.Falla(
                $"No se puede eliminar «{categoria.Nombre}»: tiene {productos.Count} producto(s) asignado(s). " +
                "Reasígnalos a otra categoría primero.");

        categoria.Activo = false;
        _categorias.Actualizar(categoria);
        await _uow.GuardarCambiosAsync(ct);
        return Result.Ok();
    }

    private async Task<bool> ExisteNombreAsync(string nombre, Guid? exceptoId, CancellationToken ct)
    {
        var objetivo = nombre.Trim();
        var existentes = await _categorias.ListarAsync(c => c.Activo, ct);
        return existentes.Any(c =>
            (exceptoId == null || c.Id != exceptoId) &&
            string.Equals(c.Nombre, objetivo, StringComparison.OrdinalIgnoreCase));
    }
}
