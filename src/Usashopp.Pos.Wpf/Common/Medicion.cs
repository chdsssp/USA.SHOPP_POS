using System.Diagnostics;
using Serilog;

namespace Usashopp.Pos.Wpf.Common;

/// <summary>
/// Telemetría local (nada sale del equipo): mide una operación y, si tarda más del umbral, lo deja
/// en el log como "Lento". Sirve para detectar en el equipo real qué se siente pesado.
/// Uso: <c>using var _ = Medicion.De("Búsqueda POS");</c>
/// </summary>
public sealed class Medicion : IDisposable
{
    private readonly string _operacion;
    private readonly long _umbralMs;
    private readonly Stopwatch _reloj = Stopwatch.StartNew();

    private Medicion(string operacion, long umbralMs)
    {
        _operacion = operacion;
        _umbralMs = umbralMs;
    }

    public static Medicion De(string operacion, long umbralMs = 300) => new(operacion, umbralMs);

    public void Dispose()
    {
        _reloj.Stop();
        if (_reloj.ElapsedMilliseconds >= _umbralMs)
            Log.Information("Lento: {Operacion} tardó {Ms} ms", _operacion, _reloj.ElapsedMilliseconds);
    }
}
