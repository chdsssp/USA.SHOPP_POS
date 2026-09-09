using Usashopp.Pos.Domain.Common;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Domain.Entities;

public class Cliente : EntidadBase, IActivable
{
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Notas { get; set; }
    public bool Activo { get; set; } = true;

    // --- Datos fiscales (CFDI) ---
    public string? Rfc { get; set; }
    public string? RazonSocial { get; set; }
    public string? RegimenFiscal { get; set; }
    public string? UsoCfdi { get; set; }
    public string? DireccionFiscal { get; set; }

    // --- Crédito (cuentas por cobrar) ---
    /// <summary>Límite de crédito autorizado (0 = sin crédito).</summary>
    public Dinero LimiteCredito { get; set; } = Dinero.Cero;

    // --- Lealtad ---
    /// <summary>Puntos de lealtad acumulados.</summary>
    public int Puntos { get; set; }
}
