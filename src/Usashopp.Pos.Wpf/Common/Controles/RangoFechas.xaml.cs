using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Usashopp.Pos.Wpf.Common.Controles;

/// <summary>
/// Botón "Periodo ▾" con rangos frecuentes (Hoy, Ayer, 7 días, Este mes, Mes pasado, Este año).
/// Fija <see cref="Desde"/>/<see cref="Hasta"/> (fechas inclusivas, sin hora) y ejecuta <see cref="Comando"/>.
/// </summary>
public partial class RangoFechas : UserControl
{
    public static readonly DependencyProperty DesdeProperty = DependencyProperty.Register(
        nameof(Desde), typeof(DateTime?), typeof(RangoFechas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public DateTime? Desde { get => (DateTime?)GetValue(DesdeProperty); set => SetValue(DesdeProperty, value); }

    public static readonly DependencyProperty HastaProperty = DependencyProperty.Register(
        nameof(Hasta), typeof(DateTime?), typeof(RangoFechas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public DateTime? Hasta { get => (DateTime?)GetValue(HastaProperty); set => SetValue(HastaProperty, value); }

    public static readonly DependencyProperty ComandoProperty = DependencyProperty.Register(
        nameof(Comando), typeof(ICommand), typeof(RangoFechas));
    public ICommand? Comando { get => (ICommand?)GetValue(ComandoProperty); set => SetValue(ComandoProperty, value); }

    public RangoFechas()
    {
        InitializeComponent();
        var hoy = DateTime.Today;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
        Opcion("Hoy", hoy, hoy);
        Opcion("Ayer", hoy.AddDays(-1), hoy.AddDays(-1));
        Opcion("Últimos 7 días", hoy.AddDays(-6), hoy);
        Opcion("Este mes", inicioMes, hoy);
        Opcion("Mes pasado", inicioMes.AddMonths(-1), inicioMes.AddDays(-1));
        Opcion("Este año", new DateTime(hoy.Year, 1, 1), hoy);
    }

    private void Opcion(string texto, DateTime desde, DateTime hasta)
    {
        var b = new Button
        {
            Content = texto,
            Style = (Style)FindResource("Button.Subtle"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left
        };
        b.Click += (_, _) =>
        {
            Desde = desde;
            Hasta = hasta;
            Menu.IsOpen = false;
            if (Comando?.CanExecute(null) == true) Comando.Execute(null);
        };
        Opciones.Children.Add(b);
    }

    private void OnAbrir(object sender, RoutedEventArgs e) => Menu.IsOpen = true;
}
