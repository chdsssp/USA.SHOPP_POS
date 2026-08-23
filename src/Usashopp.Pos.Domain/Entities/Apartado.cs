using Usashopp.Pos.Domain.Common;
using Usashopp.Pos.Domain.Enums;
using Usashopp.Pos.Domain.Exceptions;
using Usashopp.Pos.Domain.ValueObjects;

namespace Usashopp.Pos.Domain.Entities;

/// <summary>
/// Apartado (layaway): el cliente reserva productos con un anticipo y abona hasta liquidar.
/// No se liquida mientras exista saldo.
/// </summary>
public class Apartado : EntidadBase
{
    public string Folio { get; set; } = string.Empty;

    public Guid ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public DateTime? FechaLimite { get; set; }

    public EstadoApartado Estado { get; private set; } = EstadoApartado.Activo;

    public ICollection<DetalleApartado> Detalles { get; set; } = new List<DetalleApartado>();
    public ICollection<AbonoApartado> Abonos { get; set; } = new List<AbonoApartado>();

    public Dinero Total =>
        Detalles.Aggregate(Dinero.Cero, (acc, d) => acc.Mas(d.Importe));

    public Dinero TotalAbonado =>
        Abonos.Aggregate(Dinero.Cero, (acc, a) => acc.Mas(a.Monto));

    public Dinero Saldo => Total.Menos(TotalAbonado);

    public void Liquidar()
    {
        if (Saldo.Monto > 0)
            throw new DomainException($"El apartado no puede liquidarse: saldo pendiente de {Saldo}.");

        Estado = EstadoApartado.Liquidado;
    }

    public void Cancelar() => Estado = EstadoApartado.Cancelado;
}
