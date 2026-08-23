using Usashopp.Pos.Domain.Common;

namespace Usashopp.Pos.Domain.Entities;

/// <summary>
/// Configuración única de la tienda: datos del ticket, impuestos, folios y banderas
/// de operación. Solo existe una fila.
/// </summary>
public class ConfiguracionTienda : EntidadBase
{
    // Datos del ticket
    public string NombreTienda { get; set; } = "test_tienda";
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Rfc { get; set; }
    public string? MensajePieTicket { get; set; }

    // Impuestos
    public decimal TasaImpuesto { get; set; } = 0.16m;   // IVA 16%
    public bool ImpuestoIncluidoEnPrecio { get; set; } = true;

    // Operación
    public bool PermitirVentaStockNegativo { get; set; }

    // Folios (consecutivos por tipo de documento)
    public string PrefijoFolioVenta { get; set; } = "V-";
    public string PrefijoFolioApartado { get; set; } = "A-";
    public string PrefijoFolioCompra { get; set; } = "C-";
    public int ConsecutivoVenta { get; set; } = 1;
    public int ConsecutivoApartado { get; set; } = 1;
    public int ConsecutivoCompra { get; set; } = 1;
}
