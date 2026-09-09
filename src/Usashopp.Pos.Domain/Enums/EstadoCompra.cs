namespace Usashopp.Pos.Domain.Enums;

public enum EstadoCompra
{
    Borrador = 0,
    Recibida = 1,
    Cancelada = 2,
    /// <summary>Orden emitida al proveedor; aún no se recibe mercancía.</summary>
    Ordenada = 3,
    /// <summary>Se recibió parte de la mercancía ordenada.</summary>
    RecibidaParcial = 4
}
