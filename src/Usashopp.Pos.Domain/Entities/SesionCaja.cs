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

    public EstadoSesionCaja Estado { get; private set; } = EstadoSesionCaja.Abierta;

    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();

    public bool EstaAbierta => Estado == EstadoSesionCaja.Abierta;

    public void Cerrar(Dinero montoContado, DateTime fechaCierre)
    {
        MontoContado = montoContado;
        FechaCierre = fechaCierre;
        Estado = EstadoSesionCaja.Cerrada;
    }
}
