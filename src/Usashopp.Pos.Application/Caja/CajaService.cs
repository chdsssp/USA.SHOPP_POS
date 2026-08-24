using Usashopp.Pos.Application.Caja.Dtos;
using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Interfaces.System;
using Usashopp.Pos.Application.Common.Models;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Application.Caja;

/// <summary>Apertura y cierre (corte) de la sesión de caja.</summary>
public class CajaService
{
    private readonly ISesionCajaRepository _sesiones;
    private readonly IVentaRepository _ventas;
    private readonly IBackupService _backup;
    private readonly ICurrentUser _usuario;
    private readonly IDateTime _reloj;
    private readonly IUnitOfWork _uow;

    public CajaService(
        ISesionCajaRepository sesiones,
        IVentaRepository ventas,
        IBackupService backup,
        ICurrentUser usuario,
        IDateTime reloj,
        IUnitOfWork uow)
    {
        _sesiones = sesiones;
        _ventas = ventas;
        _backup = backup;
        _usuario = usuario;
        _reloj = reloj;
        _uow = uow;
    }

    /// <summary>Resumen del corte de la sesión abierta (o null si no hay caja abierta).</summary>
    public async Task<CorteCajaDto?> ObtenerCorteAsync(CancellationToken ct = default)
    {
        var sesion = await _sesiones.ObtenerSesionAbiertaAsync(ct);
        if (sesion is null) return null;

        var ventas = (await _ventas.ListarPorSesionAsync(sesion.Id, ct))
            .Where(v => v.Estado != EstadoVenta.Cancelada)
            .ToList();

        var totalVentas = ventas.Sum(v => v.Total.Monto);
        var totalEfectivo = ventas
            .SelectMany(v => v.Pagos)
            .Where(p => p.Metodo == MetodoPago.Efectivo)
            .Sum(p => p.Monto.Monto);

        return new CorteCajaDto(
            sesion.FondoInicial.Monto,
            ventas.Count,
            totalVentas,
            totalEfectivo,
            sesion.FondoInicial.Monto + totalEfectivo);
    }

    /// <summary>Historial de cortes: sesiones cerradas con su resumen y diferencia.</summary>
    public async Task<IReadOnlyList<CorteHistorialDto>> ListarCortesAsync(CancellationToken ct = default)
    {
        var sesiones = await _sesiones.ListarCerradasAsync(ct);

        return sesiones.Select(s =>
        {
            var ventas = s.Ventas.Where(v => v.Estado != EstadoVenta.Cancelada).ToList();
            var totalVentas = ventas.Sum(v => v.Total.Monto);
            var totalEfectivo = ventas
                .SelectMany(v => v.Pagos)
                .Where(p => p.Metodo == MetodoPago.Efectivo)
                .Sum(p => p.Monto.Monto);
            var esperado = s.FondoInicial.Monto + totalEfectivo;
            var contado = s.MontoContado?.Monto ?? 0m;

            return new CorteHistorialDto(
                s.FechaApertura, s.FechaCierre, s.FondoInicial.Monto,
                ventas.Count, totalVentas, totalEfectivo, esperado, contado, contado - esperado);
        }).ToList();
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

        // Respaldo automático al cerrar caja (best-effort: no debe impedir el corte).
        try { await _backup.CrearRespaldoAsync(ct); } catch { /* se registra en logging de infraestructura */ }

        return Result.Ok();
    }
}
