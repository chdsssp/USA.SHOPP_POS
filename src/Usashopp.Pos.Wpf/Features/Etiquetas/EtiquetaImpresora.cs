using System.Printing;
using System.Windows;
using System.Windows.Controls;

namespace Usashopp.Pos.Wpf.Features.Etiquetas;

/// <summary>Imprime una etiqueta 4×6" (un visual WPF) en una impresora de Windows por su nombre.</summary>
public static class EtiquetaImpresora
{
    private const double Ancho = 384; // 4" a 96 DPI
    private const double Alto = 576;  // 6" a 96 DPI

    public static void Imprimir(EtiquetaDatos datos, string impresora)
    {
        if (string.IsNullOrWhiteSpace(impresora))
            throw new InvalidOperationException("No hay una impresora de etiquetas configurada.");

        // Se renderiza un control nuevo al tamaño exacto de la etiqueta.
        var control = new EtiquetaControl { DataContext = datos };
        var tam = new Size(Ancho, Alto);
        control.Measure(tam);
        control.Arrange(new Rect(tam));
        control.UpdateLayout();

        using var server = new LocalPrintServer();
        var cola = server.GetPrintQueue(impresora); // lanza si no existe

        var pd = new PrintDialog { PrintQueue = cola };
        try { pd.PrintTicket.PageMediaSize = new PageMediaSize(Ancho, Alto); }
        catch { /* el driver puede no permitir fijar el tamaño; se imprime igual */ }

        pd.PrintVisual(control, "Etiqueta de producto");
    }
}
