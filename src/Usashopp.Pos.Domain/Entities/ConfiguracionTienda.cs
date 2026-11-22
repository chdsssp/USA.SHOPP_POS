using Usashopp.Pos.Domain.Common;

namespace Usashopp.Pos.Domain.Entities;

/// <summary>
/// Configuración única de la tienda: datos del ticket, impuestos, folios y banderas
/// de operación. Solo existe una fila.
/// </summary>
public class ConfiguracionTienda : EntidadBase
{
    // Datos del ticket
    public string NombreTienda { get; set; } = "Mi tienda";
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Rfc { get; set; }
    public string? MensajePieTicket { get; set; }

    /// <summary>Nombre del archivo del logo (en la carpeta de imágenes); null si no hay.</summary>
    public string? LogoRuta { get; set; }

    /// <summary>
    /// Nombre de la impresora de Windows (ESC/POS) a la que se envían los tickets y el pulso
    /// de apertura del cajón. Null o vacío = no imprimir (solo vista previa en pantalla).
    /// </summary>
    public string? ImpresoraTicket { get; set; }

    /// <summary>
    /// Nombre de la impresora de Windows para las etiquetas de producto (4×6"). Null o vacío =
    /// no hay impresora de etiquetas configurada.
    /// </summary>
    public string? ImpresoraEtiquetas { get; set; }

    // Impuestos
    public decimal TasaImpuesto { get; set; } = 0.16m;   // IVA 16%
    public bool ImpuestoIncluidoEnPrecio { get; set; } = true;

    // Operación
    public bool PermitirVentaStockNegativo { get; set; }

    // Lealtad: 1 punto por cada PesosPorPunto del total (antes fijo en $10 en el código).
    public bool LealtadActiva { get; set; } = true;
    public decimal PesosPorPunto { get; set; } = 10m;

    /// <summary>Días máximos desde la venta para aceptar un cambio de talla o devolución desde el POS (0 = sin límite).</summary>
    public int DiasMaximosCambio { get; set; } = 30;

    // Folios (consecutivos por tipo de documento)
    public string PrefijoFolioVenta { get; set; } = "V-";
    public string PrefijoFolioApartado { get; set; } = "A-";
    public string PrefijoFolioCompra { get; set; } = "C-";
    public int ConsecutivoVenta { get; set; } = 1;
    public int ConsecutivoApartado { get; set; } = 1;
    public int ConsecutivoCompra { get; set; } = 1;

    /// <summary>Si ya se completó el asistente de primera configuración.</summary>
    public bool ConfiguracionCompletada { get; set; }
}
