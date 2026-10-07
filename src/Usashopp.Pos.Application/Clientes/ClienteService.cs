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
        var lista = await _clientes.ListarAsync(c => c.Activo, ct);

        // Búsqueda por cualquier propiedad (nombre, teléfono, email, RFC, razón social, notas),
        // tolerante a acentos/mayúsculas y por varias palabras.
        var tokens = Common.BusquedaTexto.Tokens(texto);
        if (tokens.Length > 0)
            lista = lista.Where(c => Common.BusquedaTexto.Coincide(tokens,
                c.Nombre, c.Telefono, c.Email, c.Rfc, c.RazonSocial, c.Notas)).ToList();

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
            Notas = dto.Notas,
            Rfc = dto.Rfc,
            RazonSocial = dto.RazonSocial,
            RegimenFiscal = dto.RegimenFiscal,
            UsoCfdi = dto.UsoCfdi,
            DireccionFiscal = dto.DireccionFiscal,
            LimiteCredito = new Domain.ValueObjects.Dinero(dto.LimiteCredito)
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
        cliente.Rfc = dto.Rfc;
        cliente.RazonSocial = dto.RazonSocial;
        cliente.RegimenFiscal = dto.RegimenFiscal;
        cliente.UsoCfdi = dto.UsoCfdi;
        cliente.DireccionFiscal = dto.DireccionFiscal;
        cliente.LimiteCredito = new Domain.ValueObjects.Dinero(dto.LimiteCredito);
        // Puntos no se editan aquí (los gestiona la lealtad).
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

    private static ClienteDto Map(Cliente c) => new(
        c.Id, c.Nombre, c.Telefono, c.Email, c.Notas, c.Activo,
        c.Rfc, c.RazonSocial, c.RegimenFiscal, c.UsoCfdi, c.DireccionFiscal,
        c.LimiteCredito.Monto, c.Puntos);
}
