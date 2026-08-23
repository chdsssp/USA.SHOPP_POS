using Usashopp.Pos.Application.Common.Interfaces;

namespace Usashopp.Pos.Infrastructure.System;

/// <summary>
/// Reloj del sistema. La hora local del equipo debe estar configurada en la zona de la
/// tienda (GMT-07:00 Mazatlán); los registros se guardan en UTC.
/// </summary>
public class SystemDateTime : IDateTime
{
    public DateTime Ahora => DateTime.Now;
    public DateTime UtcAhora => DateTime.UtcNow;
}
