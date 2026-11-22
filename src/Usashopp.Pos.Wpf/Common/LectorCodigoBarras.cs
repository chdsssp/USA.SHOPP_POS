using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Usashopp.Pos.Wpf.Common;

/// <summary>
/// Detecta la lectura de un lector de códigos de barras tipo "keyboard wedge" (teclea muy rápido
/// y termina con Enter) en TODA la vista, sin importar dónde esté el foco:
/// <list type="bullet">
/// <item>Foco fuera de un campo de texto → se redirige a la búsqueda (sirve para lector y teclado).</item>
/// <item>Foco en otro campo (cantidad, notas…) → si llega una ráfaga, se restaura el texto que tenía
/// ese campo y el código se entrega a <c>alLeer</c>. Así un escaneo nunca termina como "cantidad 7501…".</item>
/// </list>
/// </summary>
public sealed class LectorCodigoBarras
{
    /// <summary>Máximo entre teclas para considerarlas parte de una ráfaga del lector.</summary>
    public static TimeSpan IntervaloMaximo { get; set; } = TimeSpan.FromMilliseconds(50);

    /// <summary>Mínimo de caracteres para que una ráfaga cuente como código.</summary>
    public const int LongitudMinima = 4;

    private readonly TextBox _busqueda;
    private readonly Func<string, Task> _alLeer;
    private readonly StringBuilder _buffer = new();
    private DateTime _ultima = DateTime.MinValue;
    private TextBox? _campoAfectado;
    private string? _textoPrevio;
    private int _caretPrevio;

    public LectorCodigoBarras(FrameworkElement raiz, TextBox busqueda, Func<string, Task> alLeer)
    {
        _busqueda = busqueda;
        _alLeer = alLeer;
        raiz.PreviewTextInput += OnPreviewTextInput;
        raiz.PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text) || char.IsControl(e.Text[0])) return;

        var ahora = DateTime.UtcNow;
        var enRafaga = ahora - _ultima <= IntervaloMaximo;
        _ultima = ahora;
        var foco = Keyboard.FocusedElement;

        if (!enRafaga)
        {
            _buffer.Clear();
            _campoAfectado = foco is TextBox tb && !ReferenceEquals(tb, _busqueda) ? tb : null;
            _textoPrevio = _campoAfectado?.Text;
            _caretPrevio = _campoAfectado?.CaretIndex ?? 0;
        }
        _buffer.Append(e.Text);

        // Sin campo de texto enfocado: lo tecleado va a la búsqueda.
        if (foco is not TextBox && foco is not PasswordBox)
        {
            _busqueda.Focus();
            _busqueda.Text += e.Text;
            _busqueda.CaretIndex = _busqueda.Text.Length;
            e.Handled = true;
            return;
        }

        // Ráfaga dentro de otro campo: a partir del 2º carácter rápido se deja de escribir en él.
        if (_campoAfectado is not null && enRafaga)
            e.Handled = true;
    }

    private async void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter && e.Key != Key.Return) return;

        var rapida = DateTime.UtcNow - _ultima <= IntervaloMaximo * 2;
        if (_campoAfectado is null || !rapida || _buffer.Length < LongitudMinima) return;

        // Fue el lector dentro de otro campo: restaurar ese campo y procesar el código.
        var codigo = _buffer.ToString();
        _campoAfectado.Text = _textoPrevio ?? string.Empty;
        _campoAfectado.CaretIndex = Math.Min(_caretPrevio, _campoAfectado.Text.Length);
        _campoAfectado = null;
        _buffer.Clear();
        e.Handled = true;
        await _alLeer(codigo);
    }
}
