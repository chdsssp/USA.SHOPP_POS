using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Usashopp.Pos.Wpf.Tests;

/// <summary>
/// Regla de CLAUDE.md: nada de tamaños de letra ni colores fijos en las vistas; todo por tokens
/// (Themes/Tokens.xaml). Así un cambio de escala o de paleta se hace en un solo lugar.
/// Exentos: Themes/ (donde viven los tokens) y los controles que representan papel impreso
/// (etiquetas), cuyos tamaños son físicos.
/// </summary>
public class TokensXamlTests
{
    private static readonly Regex Prohibido = new(
        @"FontSize=""\d|(Background|Foreground|BorderBrush|Fill|Stroke|Color)=""(#[0-9A-Fa-f]{3,8}|White|Black|Red|Gray|Green|Blue)""|Property=""FontSize"" Value=""\d",
        RegexOptions.Compiled);

    [Fact]
    public void Las_vistas_no_tienen_tamanos_ni_colores_fijos()
    {
        var raiz = BuscarRaiz();
        var features = Path.Combine(raiz, "src", "Usashopp.Pos.Wpf", "Features");
        var violaciones = Directory.EnumerateFiles(features, "*.xaml", SearchOption.AllDirectories)
            .Where(f => !Path.GetFileName(f).StartsWith("Etiqueta", StringComparison.Ordinal) ||
                        !Path.GetFileName(f).EndsWith("Control.xaml", StringComparison.Ordinal))
            .SelectMany(f => File.ReadAllLines(f)
                .Select((l, i) => (f, i, l))
                .Where(x => Prohibido.IsMatch(x.l))
                .Select(x => $"{Path.GetRelativePath(raiz, x.f)}:{x.i + 1}: {x.l.Trim()}"))
            .ToList();

        violaciones.Should().BeEmpty("usa tokens de Themes/Tokens.xaml (Font.*, Brush.*)");
    }

    private static string BuscarRaiz()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Usashopp.Pos.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("No se encontró Usashopp.Pos.sln");
    }
}
