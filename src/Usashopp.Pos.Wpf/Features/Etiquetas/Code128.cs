using System.Collections.Generic;

namespace Usashopp.Pos.Wpf.Features.Etiquetas;

/// <summary>
/// Codificador de código de barras Code 128 (subconjunto B, con dígito de control). Devuelve la
/// secuencia de anchos de módulo alternando barra/espacio (empezando por barra), lista para dibujar.
/// Code 128-B cubre dígitos, mayúsculas/minúsculas y símbolos ASCII imprimibles, así que sirve para
/// SKU o códigos numéricos arbitrarios.
/// </summary>
public static class Code128
{
    // Tabla estándar de patrones Code 128 (índices 0..106). Cada patrón son anchos de módulo
    // barra/espacio; el 106 (Stop) tiene 7 elementos, el resto 6.
    private static readonly string[] Patrones =
    {
        "212222","222122","222221","121223","121322","131222","122213","122312","132212","221213",
        "221312","231212","112232","122132","122231","113222","123122","123221","223211","221132",
        "221231","213212","223112","312131","311222","321122","321221","312212","322112","322211",
        "212123","212321","232121","111323","131123","131321","112313","132113","132311","211313",
        "231113","231311","112133","112331","132131","113123","113321","133121","313121","211331",
        "231131","213113","213311","213131","311123","311321","331121","312113","312311","332111",
        "314111","221411","431111","111224","111422","121124","121421","141122","141221","112214",
        "112412","122114","122411","142112","142211","241211","221114","413111","241112","134111",
        "111242","121142","121241","114212","124112","124211","411212","421112","421211","212141",
        "214121","412121","111143","111341","131141","114113","114311","411113","411311","113141",
        "114131","311141","411131","211412","211214","211232","2331112"
    };

    private const int InicioB = 104;
    private const int Stop = 106;

    /// <summary>Secuencia de anchos de módulo (barra, espacio, barra, …) del código completo.</summary>
    public static IReadOnlyList<int> Modulos(string texto)
    {
        texto ??= "";
        var valores = new List<int> { InicioB };
        long suma = InicioB;
        var posicion = 1;

        foreach (var c in texto)
        {
            // Code 128-B: caracteres imprimibles ASCII 32..126 → valor 0..94.
            var v = (c >= 32 && c <= 126) ? c - 32 : '?' - 32;
            valores.Add(v);
            suma += (long)v * posicion;
            posicion++;
        }

        valores.Add((int)(suma % 103)); // dígito de control
        valores.Add(Stop);

        var modulos = new List<int>();
        foreach (var v in valores)
            foreach (var ch in Patrones[v])
                modulos.Add(ch - '0');
        return modulos;
    }
}
