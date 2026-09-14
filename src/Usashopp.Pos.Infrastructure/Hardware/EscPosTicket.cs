using System.Globalization;
using System.Text;
using Usashopp.Pos.Domain.Entities;
using Usashopp.Pos.Domain.Enums;

namespace Usashopp.Pos.Infrastructure.Hardware;

/// <summary>
/// Construye la secuencia de bytes ESC/POS de un ticket para papel de 80 mm (48 columnas,
/// fuente A). El texto se transcribe a ASCII (sin acentos) para no depender de la página de
/// códigos de cada impresora.
/// </summary>
internal static class EscPosTicket
{
    private const int Ancho = 48; // columnas de una térmica de 80 mm en fuente A
    private static readonly CultureInfo Mx = CultureInfo.GetCultureInfo("es-MX");

    // --- Comandos ESC/POS ---
    private static readonly byte[] Init = { 0x1B, 0x40 };            // ESC @  (reset)
    private static readonly byte[] AlignLeft = { 0x1B, 0x61, 0x00 };
    private static readonly byte[] AlignCenter = { 0x1B, 0x61, 0x01 };
    private static readonly byte[] BoldOn = { 0x1B, 0x45, 0x01 };
    private static readonly byte[] BoldOff = { 0x1B, 0x45, 0x00 };
    private static readonly byte[] DobleTam = { 0x1D, 0x21, 0x11 };  // GS ! (doble ancho+alto)
    private static readonly byte[] TamNormal = { 0x1D, 0x21, 0x00 };
    private static readonly byte[] CortarPapel = { 0x1D, 0x56, 0x42, 0x00 }; // GS V B 0 (corte parcial con avance)

    /// <summary>Pulso de apertura de cajón (drawer-kick) por el pin 2 de la impresora.</summary>
    public static readonly byte[] AbrirCajon = { 0x1B, 0x70, 0x00, 0x19, 0xFA }; // ESC p 0 25 250

    public static byte[] Venta(
        Venta venta, string nombreTienda, string? direccion, string? telefono, string? rfc, string? mensajePie)
    {
        using var ms = new MemoryStream();
        void Raw(byte[] b) => ms.Write(b, 0, b.Length);
        void Texto(string s) { var b = Ascii(s); ms.Write(b, 0, b.Length); }
        void Linea(string s = "") { Texto(s); ms.WriteByte((byte)'\n'); }

        Raw(Init);

        // --- Encabezado ---
        Raw(AlignCenter);
        Raw(DobleTam); Raw(BoldOn);
        Linea(nombreTienda);
        Raw(TamNormal); Raw(BoldOff);
        if (!string.IsNullOrWhiteSpace(direccion)) Linea(direccion);
        if (!string.IsNullOrWhiteSpace(telefono)) Linea($"Tel: {telefono}");
        if (!string.IsNullOrWhiteSpace(rfc)) Linea($"RFC: {rfc}");

        Raw(AlignLeft);
        Linea();
        Linea(IzqDer($"Folio: {venta.Folio}", venta.Fecha.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Mx)));
        Linea(new string('-', Ancho));

        // --- Renglones ---
        foreach (var d in venta.Detalles)
        {
            foreach (var trozo in Envolver(d.Descripcion, Ancho))
                Linea(trozo);
            var izq = $"  {d.Cantidad} x {Money(d.PrecioUnitario.Monto)}";
            Linea(IzqDer(izq, Money(d.Importe.Monto)));
        }

        Linea(new string('-', Ancho));

        // --- Totales ---
        var descGlobal = venta.TotalDescuentoGlobal.Monto;
        var descLineas = venta.Detalles.Sum(d => (d.PrecioUnitario.Monto * d.Cantidad) - d.Importe.Monto);
        var descuentoTotal = descGlobal + descLineas;
        if (descuentoTotal > 0)
        {
            Linea(IzqDer("Subtotal", Money(venta.Detalles.Sum(d => d.PrecioUnitario.Monto * d.Cantidad))));
            Linea(IzqDer("Descuento", "-" + Money(descuentoTotal)));
        }

        Raw(DobleTam); Raw(BoldOn);
        Linea(IzqDer("TOTAL", Money(venta.Total.Monto), Ancho / 2));
        Raw(TamNormal); Raw(BoldOff);

        Linea(new string('-', Ancho));
        foreach (var p in venta.Pagos)
            Linea(IzqDer(EtiquetaPago(p.Metodo), Money(p.Monto.Monto)));
        if (venta.Cambio.Monto > 0)
            Linea(IzqDer("Cambio", Money(venta.Cambio.Monto)));

        // --- Pie ---
        Raw(AlignCenter);
        Linea();
        if (!string.IsNullOrWhiteSpace(mensajePie)) Linea(mensajePie);
        Linea("Gracias por su compra");

        Raw(new byte[] { 0x1B, 0x64, 0x04 }); // avanzar 4 líneas
        Raw(CortarPapel);
        return ms.ToArray();
    }

    public static byte[] Prueba(string nombreTienda)
    {
        using var ms = new MemoryStream();
        void Raw(byte[] b) => ms.Write(b, 0, b.Length);
        void Linea(string s = "") { var b = Ascii(s); ms.Write(b, 0, b.Length); ms.WriteByte((byte)'\n'); }

        Raw(Init);
        Raw(AlignCenter);
        Raw(DobleTam); Raw(BoldOn);
        Linea(nombreTienda);
        Raw(TamNormal); Raw(BoldOff);
        Linea("Impresion de prueba");
        Linea(DateTime.Now.ToString("dd/MM/yyyy HH:mm", Mx));
        Raw(AlignLeft);
        Linea(new string('-', Ancho));
        Linea("ABCDEFGHIJKLMNOPQRSTUVWXYZ 0123456789");
        Linea("Si lees esto, la impresora funciona.");
        Raw(new byte[] { 0x1B, 0x64, 0x04 });
        Raw(CortarPapel);
        return ms.ToArray();
    }

    // --- Utilidades de formato ---

    private static string Money(decimal m) => m.ToString("C2", Mx);

    private static string EtiquetaPago(MetodoPago metodo) => metodo switch
    {
        MetodoPago.Efectivo => "Efectivo",
        MetodoPago.Tarjeta => "Tarjeta",
        MetodoPago.Transferencia => "Transferencia",
        MetodoPago.Vales => "Vales",
        MetodoPago.Credito => "Credito",
        MetodoPago.NotaCredito => "Nota de credito",
        _ => "Otro"
    };

    /// <summary>Alinea <paramref name="izq"/> a la izquierda y <paramref name="der"/> a la derecha en <paramref name="ancho"/>.</summary>
    private static string IzqDer(string izq, string der, int ancho = Ancho)
    {
        var espacio = ancho - der.Length;
        if (izq.Length > espacio - 1 && espacio - 1 > 0) izq = izq[..(espacio - 1)];
        var huecos = Math.Max(1, ancho - izq.Length - der.Length);
        return izq + new string(' ', huecos) + der;
    }

    private static IEnumerable<string> Envolver(string texto, int ancho)
    {
        texto = texto?.Trim() ?? "";
        if (texto.Length == 0) { yield return ""; yield break; }
        for (var i = 0; i < texto.Length; i += ancho)
            yield return texto.Substring(i, Math.Min(ancho, texto.Length - i));
    }

    /// <summary>Transcribe a ASCII quitando acentos (é→e, ñ→n, …) para no depender de la página de códigos.</summary>
    private static byte[] Ascii(string texto)
    {
        var normal = (texto ?? "").Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normal.Length);
        foreach (var ch in normal)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(ch <= 0x7F ? ch : '?');
        }
        return Encoding.ASCII.GetBytes(sb.ToString());
    }
}
