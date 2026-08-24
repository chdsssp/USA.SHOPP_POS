using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Usashopp.Pos.Wpf.Common;

/// <summary>
/// Propiedad adjunta para restringir un TextBox a entrada numérica (dígitos y un separador
/// decimal). Uso en XAML: common:InputHelpers.SoloNumeros="True".
/// </summary>
public static class InputHelpers
{
    public static readonly DependencyProperty SoloNumerosProperty =
        DependencyProperty.RegisterAttached("SoloNumeros", typeof(bool), typeof(InputHelpers),
            new PropertyMetadata(false, OnSoloNumerosChanged));

    public static bool GetSoloNumeros(DependencyObject obj) => (bool)obj.GetValue(SoloNumerosProperty);
    public static void SetSoloNumeros(DependencyObject obj, bool value) => obj.SetValue(SoloNumerosProperty, value);

    private static readonly Regex Permitido = new(@"^[0-9]*[.,]?[0-9]*$", RegexOptions.Compiled);

    private static void OnSoloNumerosChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox tb) return;

        if ((bool)e.NewValue)
        {
            tb.PreviewTextInput += OnPreviewTextInput;
            DataObject.AddPastingHandler(tb, OnPaste);
        }
        else
        {
            tb.PreviewTextInput -= OnPreviewTextInput;
            DataObject.RemovePastingHandler(tb, OnPaste);
        }
    }

    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var tb = (TextBox)sender;
        var yaTieneSeparador = tb.Text.Contains('.') || tb.Text.Contains(',');
        foreach (var c in e.Text)
        {
            if (char.IsDigit(c)) continue;
            if ((c == '.' || c == ',') && !yaTieneSeparador) continue;
            e.Handled = true;
            return;
        }
    }

    private static void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetData(typeof(string)) is string texto && Permitido.IsMatch(texto))
            return;
        e.CancelCommand();
    }
}
