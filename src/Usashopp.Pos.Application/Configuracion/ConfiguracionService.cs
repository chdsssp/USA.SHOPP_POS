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
    bool PermitirVentaStockNegativo,
    string? LogoRuta = null);

/// <summary>Datos capturados por el asistente de primera configuración.</summary>
public record AsistenteConfiguracionDto(
    string NombreTienda,
    string? Direccion,
    string? Telefono,
    string? Rfc,
    string? MensajePieTicket,
    decimal TasaImpuesto,
    string PrefijoFolioVenta,
    string PrefijoFolioApartado,
    string PrefijoFolioCompra);

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
            c.TasaImpuesto, c.ImpuestoIncluidoEnPrecio, c.PermitirVentaStockNegativo, c.LogoRuta);
    }

    /// <summary>Indica si aún falta completar el asistente de primera configuración.</summary>
    public async Task<bool> RequiereAsistenteAsync(CancellationToken ct = default)
    {
        var c = await _configuracion.ObtenerAsync(ct);
        return !c.ConfiguracionCompletada;
    }

    /// <summary>Aplica los datos del asistente y marca la configuración como completada.</summary>
    public async Task<Result> CompletarAsistenteAsync(AsistenteConfiguracionDto dto, CancellationToken ct = default)
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
        if (!string.IsNullOrWhiteSpace(dto.PrefijoFolioVenta)) c.PrefijoFolioVenta = dto.PrefijoFolioVenta.Trim();
        if (!string.IsNullOrWhiteSpace(dto.PrefijoFolioApartado)) c.PrefijoFolioApartado = dto.PrefijoFolioApartado.Trim();
        if (!string.IsNullOrWhiteSpace(dto.PrefijoFolioCompra)) c.PrefijoFolioCompra = dto.PrefijoFolioCompra.Trim();
        c.ConfiguracionCompletada = true;
        await _uow.GuardarCambiosAsync(ct);
        return Result.Ok();
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
        c.LogoRuta = dto.LogoRuta;
        await _uow.GuardarCambiosAsync(ct);
        return Result.Ok();
    }
}
