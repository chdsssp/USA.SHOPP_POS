using Usashopp.Pos.Domain.Entities;

namespace Usashopp.Pos.Application.Clientes;

/// <summary>Regla de puntos de lealtad (configurable en la tienda; antes "1 por cada $10" fijo).</summary>
public static class Lealtad
{
    public static int PuntosPor(decimal total, ConfiguracionTienda config) =>
        !config.LealtadActiva || config.PesosPorPunto <= 0 || total <= 0
            ? 0
            : (int)(total / config.PesosPorPunto);
}
