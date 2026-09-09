using System.Text;

namespace Usashopp.Pos.Application.Common;

/// <summary>Utilidades mínimas de CSV (RFC 4180): escape de campos y parseo con comillas.</summary>
public static class CsvUtil
{
    /// <summary>Escapa un campo para CSV (comillas si contiene coma, comillas o salto de línea).</summary>
    public static string Escapar(string? campo)
    {
        var texto = campo ?? string.Empty;
        if (texto.Contains(',') || texto.Contains('"') || texto.Contains('\n') || texto.Contains('\r'))
            return $"\"{texto.Replace("\"", "\"\"")}\"";
        return texto;
    }

    /// <summary>Parsea un CSV completo en filas de campos. Respeta comillas y comas/saltos dentro de comillas.</summary>
    public static List<string[]> Parsear(string contenido)
    {
        var filas = new List<string[]>();
        var campos = new List<string>();
        var actual = new StringBuilder();
        var enComillas = false;
        var i = 0;

        // Ignora BOM inicial si viene.
        if (contenido.Length > 0 && contenido[0] == '﻿') i = 1;

        void CerrarCampo() { campos.Add(actual.ToString()); actual.Clear(); }
        void CerrarFila()
        {
            CerrarCampo();
            // Descarta filas totalmente vacías.
            if (!(campos.Count == 1 && campos[0].Length == 0))
                filas.Add(campos.ToArray());
            campos.Clear();
        }

        for (; i < contenido.Length; i++)
        {
            var c = contenido[i];
            if (enComillas)
            {
                if (c == '"')
                {
                    if (i + 1 < contenido.Length && contenido[i + 1] == '"') { actual.Append('"'); i++; }
                    else enComillas = false;
                }
                else actual.Append(c);
            }
            else
            {
                switch (c)
                {
                    case '"': enComillas = true; break;
                    case ',': CerrarCampo(); break;
                    case '\r': break; // se maneja con \n
                    case '\n': CerrarFila(); break;
                    default: actual.Append(c); break;
                }
            }
        }

        // Última fila sin salto final.
        if (actual.Length > 0 || campos.Count > 0) CerrarFila();
        return filas;
    }
}
