using Usashopp.Pos.Application.Common.Interfaces;
using Usashopp.Pos.Application.Common.Models;

namespace Usashopp.Pos.Application.Configuracion;

public record ConfiguracionDto(
    string NombreTienda,
    string? Direccion,
    string? Telefono,
    string? Rfc,
    string? MensajePieTicket,
    decimal TasaImpuesto,
    bool ImpuestoIncluidoEnPrecio,
    bool PermitirVentaStockNegativo);

/// <summary>Lectura y actualización de la configuración de la tienda.</summary>
public class ConfiguracionService
{
    private readonly IConfiguracionTiendaRepository _configuracion;
    private readonly IUnitOfWork _uow;

    public ConfiguracionService(IConfiguracionTiendaRepository configuracion, IUnitOfWork uow)
    {
        _configuracion = configuracion;
        _uow = uow;
    }

    public async Task<ConfiguracionDto> ObtenerAsync(CancellationToken ct = default)
    {
        var c = await _configuracion.ObtenerAsync(ct);
        return new ConfiguracionDto(
            c.NombreTienda, c.Direccion, c.Telefono, c.Rfc, c.MensajePieTicket,
            c.TasaImpuesto, c.ImpuestoIncluidoEnPrecio, c.PermitirVentaStockNegativo);
    }

    public async Task<Result> GuardarAsync(ConfiguracionDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.NombreTienda))
            return Result.Falla("El nombre de la tienda es obligatorio.");

        var c = await _configuracion.ObtenerAsync(ct);
        c.NombreTienda = dto.NombreTienda.Trim();
        c.Direccion = dto.Direccion;
        c.Telefono = dto.Telefono;
        c.Rfc = dto.Rfc;
        c.MensajePieTicket = dto.MensajePieTicket;
        c.TasaImpuesto = dto.TasaImpuesto;
        c.ImpuestoIncluidoEnPrecio = dto.ImpuestoIncluidoEnPrecio;
        c.PermitirVentaStockNegativo = dto.PermitirVentaStockNegativo;
        await _uow.GuardarCambiosAsync(ct);
        return Result.Ok();
    }
}
