using System.Net;
using System.Text;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The table file of <see cref="OmniHtmlEditorAction.ImportTable"/>: CSV and TSV read here (quoted cells,
/// doubled quotes and line breaks inside quotes included), every row padded to the widest, at most
/// <see cref="MaxRows"/> rows and <see cref="MaxColumns"/> columns, and the table written as HTML.
/// </summary>
internal static class HtmlEditorTableFile
{
    internal const int MaxRows = 100;
    internal const int MaxColumns = 50;
    internal const long MaxBytes = 10 * 1024 * 1024;

    internal static IReadOnlyList<string> OwnExtensions { get; } = [".csv", ".tsv"];

    internal static IReadOnlyList<IReadOnlyList<string>> ReadDelimited(string text, char delimiter)
    {
        var rows = new List<IReadOnlyList<string>>();
        var row = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < text.Length && rows.Count < MaxRows + 1; index++)
        {
            var character = text[index];
            if (quoted)
            {
                if (character != '"')
                {
                    cell.Append(character);
                }
                else if (index + 1 < text.Length && text[index + 1] == '"')
                {
                    cell.Append('"');
                    index++;
                }
                else
                {
                    quoted = false;
                }
            }
            else if (character == '"' && cell.Length == 0)
            {
                quoted = true;
            }
            else if (character == delimiter)
            {
                row.Add(cell.ToString());
                cell.Clear();
            }
            else if (character is '\r' or '\n')
            {
                if (character == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                {
                    index++;
                }

                row.Add(cell.ToString());
                cell.Clear();
                rows.Add(row);
                row = [];
            }
            else
            {
                cell.Append(character);
            }
        }

        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            rows.Add(row);
        }

        return Shape(rows);
    }

    /// <summary>Drops the blank rows, keeps the first <see cref="MaxRows"/> + 1 (a header may lead) and <see cref="MaxColumns"/>, and pads every row to the widest.</summary>
    internal static IReadOnlyList<IReadOnlyList<string>> Shape(IEnumerable<IReadOnlyList<string>> rows)
    {
        var kept = rows
            .Where(row => row.Any(cell => !string.IsNullOrWhiteSpace(cell)))
            .Take(MaxRows + 1)
            .Select(row => row.Take(MaxColumns).ToList())
            .ToList();
        var width = kept.Count == 0 ? 0 : kept.Max(row => row.Count);
        return kept.Select(row => (IReadOnlyList<string>)[.. row, .. Enumerable.Repeat(string.Empty, width - row.Count)]).ToList();
    }

    internal static string ToHtml(IReadOnlyList<IReadOnlyList<string>> rows, bool header)
    {
        var html = new StringBuilder("<table>");
        var body = rows;
        if (header && rows.Count > 0)
        {
            html.Append("<thead><tr>");
            foreach (var cell in rows[0])
            {
                html.Append("<th>").Append(WebUtility.HtmlEncode(cell)).Append("</th>");
            }

            html.Append("</tr></thead>");
            body = rows.Skip(1).ToList();
        }

        html.Append("<tbody>");
        foreach (var row in body.Take(MaxRows))
        {
            html.Append("<tr>");
            foreach (var cell in row)
            {
                html.Append("<td>").Append(WebUtility.HtmlEncode(cell)).Append("</td>");
            }

            html.Append("</tr>");
        }

        return html.Append("</tbody></table>").ToString();
    }
}
