using System.Runtime.InteropServices;

namespace Usashopp.Pos.Infrastructure.Hardware;

/// <summary>
/// Envía bytes crudos a una impresora de Windows por su nombre, usando el spooler
/// (datatype RAW). Es la vía correcta para impresoras ESC/POS instaladas como impresora
/// de Windows por USB: se saltan el driver GDI y llegan los comandos ESC/POS tal cual.
/// </summary>
internal static class RawPrinterHelper
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DOCINFOA
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string pDocName;
        [MarshalAs(UnmanagedType.LPWStr)] public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPWStr)] public string pDataType;
    }

    [DllImport("winspool.Drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool OpenPrinter(string src, out IntPtr hPrinter, IntPtr pd);

    [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, ref DOCINFOA di);

    [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    /// <summary>
    /// Envía <paramref name="bytes"/> a la impresora <paramref name="nombreImpresora"/> como
    /// documento RAW. Lanza <see cref="InvalidOperationException"/> si algo falla (impresora
    /// inexistente, sin conexión, etc.).
    /// </summary>
    public static void EnviarBytes(string nombreImpresora, byte[] bytes)
    {
        if (string.IsNullOrWhiteSpace(nombreImpresora))
            throw new InvalidOperationException("No hay una impresora configurada.");

        if (!OpenPrinter(nombreImpresora, out var hPrinter, IntPtr.Zero))
            throw new InvalidOperationException(
                $"No se pudo abrir la impresora «{nombreImpresora}» (código {Marshal.GetLastWin32Error()}).");

        var pUnmanagedBytes = IntPtr.Zero;
        try
        {
            var di = new DOCINFOA
            {
                pDocName = "USASHOPP POS Ticket",
                pOutputFile = null,
                pDataType = "RAW"
            };

            if (!StartDocPrinter(hPrinter, 1, ref di))
                throw new InvalidOperationException(
                    $"No se pudo iniciar el documento de impresión (código {Marshal.GetLastWin32Error()}).");
            try
            {
                if (!StartPagePrinter(hPrinter))
                    throw new InvalidOperationException(
                        $"No se pudo iniciar la página de impresión (código {Marshal.GetLastWin32Error()}).");
                try
                {
                    pUnmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);
                    Marshal.Copy(bytes, 0, pUnmanagedBytes, bytes.Length);
                    if (!WritePrinter(hPrinter, pUnmanagedBytes, bytes.Length, out _))
                        throw new InvalidOperationException(
                            $"No se pudieron enviar los datos a la impresora (código {Marshal.GetLastWin32Error()}).");
                }
                finally { EndPagePrinter(hPrinter); }
            }
            finally { EndDocPrinter(hPrinter); }
        }
        finally
        {
            if (pUnmanagedBytes != IntPtr.Zero) Marshal.FreeCoTaskMem(pUnmanagedBytes);
            ClosePrinter(hPrinter);
        }
    }
}
