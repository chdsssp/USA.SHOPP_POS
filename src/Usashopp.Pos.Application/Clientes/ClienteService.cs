using Usashopp.Pos.Application.Clientes.Dtos;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Application.Clientes;

public class ClienteService
{
    private readonly IRepository<Cliente> _clientes;
    private readonly IUnitOfWork _uow;

    public ClienteService(IRepository<Cliente> clientes, IUnitOfWork uow)
    {
        _clientes = clientes;
        _uow = uow;
    }

    public async Task<IReadOnlyList<ClienteDto>> ListarAsync(string? texto = null, CancellationToken ct = default)
    {
        IReadOnlyList<Cliente> lista;
        if (string.IsNullOrWhiteSpace(texto))
        {
            lista = await _clientes.ListarAsync(c => c.Activo, ct);
        }
        else
        {
            var t = texto.Trim();
            lista = await _clientes.ListarAsync(
                c => c.Activo && (c.Nombre.Contains(t) || (c.Telefono != null && c.Telefono.Contains(t))), ct);
        }
        return lista.OrderBy(c => c.Nombre).Select(Map).ToList();
    }

    public async Task<Result> CrearAsync(ClienteDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return Result.Falla("El nombre del cliente es obligatorio.");

        await _clientes.AgregarAsync(new Cliente
        {
            Nombre = dto.Nombre.Trim(),
            Telefono = dto.Telefono,
            Email = dto.Email,
            Notas = dto.Notas
        }, ct);
        await _uow.GuardarCambiosAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> ActualizarAsync(ClienteDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return Result.Falla("El nombre del cliente es obligatorio.");

        var cliente = await _clientes.ObtenerPorIdAsync(dto.Id, ct);
        if (cliente is null) return Result.Falla("El cliente no existe.");

        cliente.Nombre = dto.Nombre.Trim();
        cliente.Telefono = dto.Telefono;
        cliente.Email = dto.Email;
        cliente.Notas = dto.Notas;
        _clientes.Actualizar(cliente);
        await _uow.GuardarCambiosAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> DesactivarAsync(Guid id, CancellationToken ct = default)
    {
        var cliente = await _clientes.ObtenerPorIdAsync(id, ct);
        if (cliente is null) return Result.Falla("El cliente no existe.");
        cliente.Activo = false;
        _clientes.Actualizar(cliente);
        await _uow.GuardarCambiosAsync(ct);
        return Result.Ok();
    }

    private static ClienteDto Map(Cliente c) => new(c.Id, c.Nombre, c.Telefono, c.Email, c.Notas, c.Activo);
}
