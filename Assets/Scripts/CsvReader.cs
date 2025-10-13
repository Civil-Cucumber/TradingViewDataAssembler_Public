using System.Collections.Generic;
using System.Text.RegularExpressions;

public class CsvReader
{
    static readonly Regex CsvSplit = new(@",(?=(?:[^""]*""[^""]*"")*[^""]*$)");
    static readonly Regex LineBreaks = new(@"\r?\n");
    static readonly Regex TrailingMarkers = new(@";[A-Z]*\s*$");              // ;, ;O, ;IA, ...
    static readonly Regex WholeLineQuotes = new(@"^\s*""(.*)""\s*$", RegexOptions.Singleline);
    static readonly char[] TrimChars = { '"' };
    
    static string NormalizeLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return line;

        // 1) ;*, ;O, ;IA, ... entfernen
        line = TrailingMarkers.Replace(line, "");

        // 2) ganze Zeile in Quotes?
        var m = WholeLineQuotes.Match(line);
        if (m.Success)
        {
            // 2a) äußere Quotes ab
            line = m.Groups[1].Value;
            // 3) verdoppelte Quotes zu einfachen
            line = line.Replace(@"""""", @"""");
        }

        return line;
    }

    public static List<Dictionary<string, string>> Read(string text)
    {
        var list = new List<Dictionary<string, string>>();
        var lines = LineBreaks.Split(text);
        if (lines.Length == 0) return list;

        // Header normalisieren
        var headerLine = NormalizeLine(lines[0]);
        if (string.IsNullOrWhiteSpace(headerLine)) return list;
        var header = CsvSplit.Split(headerLine);

        for (int i = 1; i < lines.Length; i++)
        {
            var line = NormalizeLine(lines[i]);
            if (string.IsNullOrWhiteSpace(line)) continue;

            var values = CsvSplit.Split(line);
            if (values.Length == 0 || string.IsNullOrEmpty(values[0])) continue;

            var entry = new Dictionary<string, string>(header.Length);
            for (int j = 0; j < header.Length && j < values.Length; j++)
            {
                var v = values[j].Trim();
                // Feldweise Quotes entfernen + interne "" ent-escapen
                if (v.Length >= 2 && v[0] == '"' && v[^1] == '"')
                    v = v.Substring(1, v.Length - 2).Replace(@"""""", @"""");

                entry[header[j]] = v;
            }
            list.Add(entry);
        }
        return list;
    }
}