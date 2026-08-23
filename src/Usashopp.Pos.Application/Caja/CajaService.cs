using Usashopp.Pos.Application.Caja.Dtos;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Application.Caja;

/// <summary>Apertura y cierre (corte) de la sesión de caja.</summary>
public class CajaService
{
    private readonly ISesionCajaRepository _sesiones;
    private readonly ICurrentUser _usuario;
    private readonly IDateTime _reloj;
    private readonly IUnitOfWork _uow;

    public CajaService(
        ISesionCajaRepository sesiones,
        ICurrentUser usuario,
        IDateTime reloj,
        IUnitOfWork uow)
    {
        _sesiones = sesiones;
        _usuario = usuario;
        _reloj = reloj;
        _uow = uow;
    }

    public async Task<SesionCajaDto?> ObtenerAbiertaAsync(CancellationToken ct = default)
    {
        var sesion = await _sesiones.ObtenerSesionAbiertaAsync(ct);
        return sesion is null
            ? null
            : new SesionCajaDto(sesion.Id, sesion.FechaApertura, sesion.FondoInicial.Monto);
    }

    public async Task<Result<SesionCajaDto>> AbrirAsync(decimal fondoInicial, CancellationToken ct = default)
    {
        if (fondoInicial < 0)
            return Result.Falla<SesionCajaDto>("El fondo inicial no puede ser negativo.");

        if (await _sesiones.ObtenerSesionAbiertaAsync(ct) is not null)
            return Result.Falla<SesionCajaDto>("Ya hay una caja abierta.");

        var sesion = new SesionCaja
        {
            UsuarioId = _usuario.UsuarioId ?? Guid.Empty,
            FechaApertura = _reloj.UtcAhora,
            FondoInicial = new Dinero(fondoInicial)
        };

        await _sesiones.AgregarAsync(sesion, ct);
        await _uow.GuardarCambiosAsync(ct);

        return Result.Ok(new SesionCajaDto(sesion.Id, sesion.FechaApertura, sesion.FondoInicial.Monto));
    }

    public async Task<Result> CerrarAsync(decimal montoContado, CancellationToken ct = default)
    {
        var sesion = await _sesiones.ObtenerSesionAbiertaAsync(ct);
        if (sesion is null)
            return Result.Falla("No hay una caja abierta.");

        sesion.Cerrar(new Dinero(montoContado), _reloj.UtcAhora);
        _sesiones.Actualizar(sesion);
        await _uow.GuardarCambiosAsync(ct);
        return Result.Ok();
    }
}
