using Usashopp.Pos.Domain.Common;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Domain.Entities;

/// <summary>
/// Sesión de caja: apertura con fondo y cierre (corte) con conteo. Solo puede haber
/// una sesión abierta a la vez.
/// </summary>
public class SesionCaja : EntidadBase
{
    public Guid UsuarioId { get; set; }
    public DateTime FechaApertura { get; set; } = DateTime.UtcNow;
    public Dinero FondoInicial { get; set; } = Dinero.Cero;

    public DateTime? FechaCierre { get; set; }
    public Dinero? MontoContado { get; set; }

    // --- Corte congelado al cerrar ---
    // Se calculan al momento del cierre y quedan inmutables, para que el historial no cambie
    // retroactivamente si más tarde se cancela una venta de esta sesión. Nulos en sesiones
    // cerradas antes de introducir el snapshot (el historial recalcula en ese caso).
    public int? CorteNumVentas { get; set; }
    public Dinero? CorteTotalVentas { get; set; }
    public Dinero? CorteTotalEfectivo { get; set; }
    public Dinero? CorteEfectivoEsperado { get; set; }

    public EstadoSesionCaja Estado { get; private set; } = EstadoSesionCaja.Abierta;

    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();

    public bool EstaAbierta => Estado == EstadoSesionCaja.Abierta;

    /// <summary>Cierra la sesión y congela el resumen del corte (queda inmutable).</summary>
    public void Cerrar(
        Dinero montoContado,
        DateTime fechaCierre,
        int numVentas,
        Dinero totalVentas,
        Dinero totalEfectivo,
        Dinero efectivoEsperado)
    {
        MontoContado = montoContado;
        FechaCierre = fechaCierre;
        CorteNumVentas = numVentas;
        CorteTotalVentas = totalVentas;
        CorteTotalEfectivo = totalEfectivo;
        CorteEfectivoEsperado = efectivoEsperado;
        Estado = EstadoSesionCaja.Cerrada;
    }
}
