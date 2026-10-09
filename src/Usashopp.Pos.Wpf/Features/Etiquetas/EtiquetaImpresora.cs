using System.Printing;
using System.Windows;
using System.Windows.Controls;

namespace Usashopp.Pos.Wpf.Features.Etiquetas;

/// <summary>Imprime una hoja de etiquetas (visual WPF) en una impresora de Windows por su nombre.</summary>
public static class EtiquetaImpresora
{
    public static void Imprimir(EtiquetaDatos datos, TamanoEtiqueta tam, string impresora)
    {
        if (string.IsNullOrWhiteSpace(impresora))
            throw new InvalidOperationException("No hay una impresora de etiquetas configurada.");

        // Se construye y renderiza la hoja al tamaño exacto del papel elegido.
        var control = EtiquetaVisualFactory.Construir(datos, tam);
        var tamano = new Size(tam.AnchoPx, tam.AltoPx);
        control.Measure(tamano);
        control.Arrange(new Rect(tamano));
        control.UpdateLayout();

        using var server = new LocalPrintServer();
        var cola = server.GetPrintQueue(impresora); // lanza si no existe

        var pd = new PrintDialog { PrintQueue = cola };
        try { pd.PrintTicket.PageMediaSize = new PageMediaSize(tam.AnchoPx, tam.AltoPx); }
        catch { /* el driver puede no permitir fijar el tamaño; se imprime igual */ }

        pd.PrintVisual(control, "Etiqueta de producto");
    }
}
