using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;

namespace Usashopp.Pos.Domain.Services;

/// <summary>Una línea del carrito vista por el motor de promociones.</summary>
public readonly record struct LineaPromo(
    int Indice,
    Guid VarianteId,
    Guid? ProductoId,
    Guid? CategoriaId,
    int Cantidad,
    decimal PrecioUnitario,
    bool TieneDescuentoManual);

/// <summary>Resultado por línea: cuánto descuenta la promoción y cuál fue.</summary>
public readonly record struct PromoAplicada(int Indice, decimal Descuento, Guid PromocionId, string Nombre);

/// <summary>
/// Motor de promociones (puro, sin IO): decide qué promoción aplica a cada línea y cuánto descuenta.
/// Reglas:
/// <list type="bullet">
/// <item>A cada línea le toca UNA promoción: la vigente de mayor prioridad cuyo alcance la incluye
/// (a igual prioridad, la más específica: variante, producto, categoría, toda la tienda).</item>
/// <item>Si la línea tiene descuento manual y la promoción no es acumulable, el manual la reemplaza.</item>
/// <item>N×M y N por $X agrupan las piezas de TODAS las líneas con esa promoción (p. ej. 2×1 entre una
/// playera negra M y una blanca L); en N×M se regalan las más baratas.</item>
/// <item>El descuento nunca deja una línea por debajo de 0.</item>
/// </list>
/// </summary>
public static class MotorPromociones
{
    public static IReadOnlyList<PromoAplicada> Aplicar(
        IReadOnlyList<LineaPromo> lineas, IEnumerable<Promocion> promociones, DateTime fechaLocal)
    {
        var vigentes = promociones.Where(p => p.VigenteEn(fechaLocal)).ToList();
        if (vigentes.Count == 0 || lineas.Count == 0) return Array.Empty<PromoAplicada>();

        // 1) Promoción elegida para cada línea.
        var asignadas = new List<(LineaPromo Linea, Promocion Promo)>();
        foreach (var l in lineas.Where(l => l.Cantidad > 0 && l.PrecioUnitario > 0))
        {
            var promo = vigentes
                .Where(p => Incluye(p, l))
                .Where(p => !l.TieneDescuentoManual || p.AcumulableConManual)
                .OrderByDescending(p => p.Prioridad)
                .ThenByDescending(p => (int)p.Alcance)
                .FirstOrDefault();
            if (promo is not null) asignadas.Add((l, promo));
        }

        // 2) Descuento por grupo de promoción.
        var resultado = new List<PromoAplicada>();
        foreach (var grupo in asignadas.GroupBy(a => a.Promo.Id))
        {
            var p = grupo.First().Promo;
            var del = grupo.Select(g => g.Linea).ToList();
            var descuentos = p.Tipo switch
            {
                TipoPromocion.Porcentaje => del.ToDictionary(l => l.Indice, l => l.PrecioUnitario * l.Cantidad * Math.Clamp(p.Valor, 0, 100) / 100m),
                TipoPromocion.MontoPorPieza => del.ToDictionary(l => l.Indice, l => Math.Min(p.Valor, l.PrecioUnitario) * l.Cantidad),
                TipoPromocion.PrecioEspecial => del.ToDictionary(l => l.Indice, l => Math.Max(0, l.PrecioUnitario - p.Valor) * l.Cantidad),
                TipoPromocion.NxM => PorPaquetes(del, p.N, piezas => piezas.OrderBy(x => x).Take(Math.Max(0, p.N - p.M)).Sum()),
                TipoPromocion.NPorPrecio => PorPaquetes(del, p.N, piezas => Math.Max(0, piezas.Sum() - p.Valor)),
                _ => new Dictionary<int, decimal>()
            };

            foreach (var l in del)
            {
                var d = Math.Round(Math.Min(descuentos.GetValueOrDefault(l.Indice), l.PrecioUnitario * l.Cantidad), 2);
                if (d > 0) resultado.Add(new PromoAplicada(l.Indice, d, p.Id, p.Nombre));
            }
        }
        return resultado;
    }

    private static bool Incluye(Promocion p, LineaPromo l) => p.Alcance switch
    {
        AlcancePromocion.Todo => true,
        AlcancePromocion.Categoria => p.AlcanceId == l.CategoriaId,
        AlcancePromocion.Producto => p.AlcanceId == l.ProductoId,
        AlcancePromocion.Variante => p.AlcanceId == l.VarianteId,
        _ => false
    };

    /// <summary>
    /// Arma paquetes de N piezas (de la más cara a la más barata) y reparte el descuento de cada
    /// paquete entre sus piezas en proporción a su precio. Las piezas que no completan paquete no
    /// llevan descuento.
    /// </summary>
    private static Dictionary<int, decimal> PorPaquetes(List<LineaPromo> lineas, int n, Func<List<decimal>, decimal> descuentoPaquete)
    {
        var res = new Dictionary<int, decimal>();
        if (n <= 1) return res;
        var piezas = lineas.SelectMany(l => Enumerable.Repeat((l.Indice, l.PrecioUnitario), l.Cantidad))
                           .OrderByDescending(x => x.PrecioUnitario).ToList();
        for (var i = 0; i + n <= piezas.Count; i += n)
        {
            var paquete = piezas.GetRange(i, n);
            var precios = paquete.Select(x => x.PrecioUnitario).ToList();
            var descuento = descuentoPaquete(precios);
            var suma = precios.Sum();
            if (descuento <= 0 || suma <= 0) continue;
            foreach (var (indice, precio) in paquete)
                res[indice] = res.GetValueOrDefault(indice) + descuento * precio / suma;
        }
        return res;
    }
}
