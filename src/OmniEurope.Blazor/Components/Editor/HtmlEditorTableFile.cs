using System.Net;
using System.Text;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The table file of <see cref="OmniHtmlEditorAction.ImportTable"/>: CSV and TSV read here (quoted cells,
/// doubled quotes and line breaks inside quotes included), every row padded to the widest, at most
/// <see cref="MaxRows"/> rows and <see cref="MaxColumns"/> columns, and the table written as HTML. Blank rows do
/// not count toward the limit, and a file that goes beyond it is reported as truncated, never cut silently.
/// </summary>
internal static class HtmlEditorTableFile
{
    internal const int MaxRows = 100;
    internal const int MaxColumns = 50;
    internal const long MaxBytes = 10 * 1024 * 1024;

    /// <summary>
    /// The most bytes of a CSV or TSV file that are read: 100 rows of 50 cells fit in far less, so a larger file
    /// is read up to here and reported as truncated rather than copied whole into memory and decoded.
    /// </summary>
    internal const int MaxTextBytes = 2 * 1024 * 1024;

    internal static IReadOnlyList<string> OwnExtensions { get; } = [".csv", ".tsv"];

    /// <summary>
    /// Reads at most <see cref="MaxTextBytes"/> of a CSV or TSV stream as text. A larger stream is cut at its last
    /// complete line and reported as truncated, so no half row is ever taken for a whole one.
    /// </summary>
    internal static async Task<(string Text, bool Truncated)> ReadTextAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var content = new MemoryStream();
        var chunk = new byte[81920];
        while (content.Length <= MaxTextBytes)
        {
            var read = await stream.ReadAsync(chunk, cancellationToken);
            if (read == 0)
            {
                break;
            }

            content.Write(chunk, 0, (int)Math.Min(read, MaxTextBytes + 1L - content.Length));
        }

        var truncated = content.Length > MaxTextBytes;
        content.SetLength(Math.Min(content.Length, MaxTextBytes));
        content.Position = 0;
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var text = await reader.ReadToEndAsync(cancellationToken);
        if (truncated)
        {
            text = text[..(text.AsSpan().LastIndexOfAny('\r', '\n') + 1)];
        }

        return (text, truncated);
    }

    /// <summary>
    /// Reads the rows of a CSV or TSV text. Blank rows are left out as they are read, so they never take the place of
    /// a real row; <paramref name="truncated"/> says more rows than <see cref="MaxRows"/> (and a header), or wider ones
    /// than <see cref="MaxColumns"/>, were cut.
    /// </summary>
    internal static IReadOnlyList<IReadOnlyList<string>> ReadDelimited(string text, char delimiter, out bool truncated)
    {
        var reader = new DelimitedReader(delimiter);

        // One row more than kept is read, to know the file went beyond the limit.
        for (var index = 0; index < text.Length && reader.Rows.Count < MaxRows + 2; index++)
        {
            index = reader.Read(text, index);
        }

        reader.Finish();
        return Shape(reader.Rows, out truncated);
    }

    /// <summary>The state of a CSV or TSV read: the rows done, the row and the cell being filled, and whether a quote is open.</summary>
    private sealed class DelimitedReader(char delimiter)
    {
        private List<string> _row = [];
        private readonly StringBuilder _cell = new();
        private bool _quoted;

        internal List<IReadOnlyList<string>> Rows { get; } = [];

        /// <summary>Reads the character at <paramref name="index"/>; returns the index of the last character it used.</summary>
        internal int Read(string text, int index)
        {
            var character = text[index];
            if (_quoted)
            {
                return ReadQuoted(text, index);
            }

            if (character == '"' && _cell.Length == 0)
            {
                _quoted = true;
            }
            else if (character == delimiter)
            {
                _row.Add(_cell.ToString());
                _cell.Clear();
            }
            else if (character is '\r' or '\n')
            {
                // A CRLF pair ends the row once.
                var pair = character == '\r' && index + 1 < text.Length && text[index + 1] == '\n';
                EndRow();
                return pair ? index + 1 : index;
            }
            else
            {
                _cell.Append(character);
            }

            return index;
        }

        /// <summary>The row a text ends on, when it has no final line break.</summary>
        internal void Finish()
        {
            if (_cell.Length > 0 || _row.Count > 0)
            {
                EndRow();
            }
        }

        private int ReadQuoted(string text, int index)
        {
            var character = text[index];
            if (character != '"')
            {
                _cell.Append(character);
            }
            else if (index + 1 < text.Length && text[index + 1] == '"')
            {
                // A doubled quote is one literal quote.
                _cell.Append('"');
                return index + 1;
            }
            else
            {
                _quoted = false;
            }

            return index;
        }

        private void EndRow()
        {
            _row.Add(_cell.ToString());
            _cell.Clear();
            if (_row.Any(value => !string.IsNullOrWhiteSpace(value)))
            {
                Rows.Add(_row);
            }

            _row = [];
        }
    }

    /// <summary>
    /// Drops the blank rows, keeps the first <see cref="MaxRows"/> + 1 (a header may lead) and <see cref="MaxColumns"/>, and
    /// pads every row to the widest. <paramref name="truncated"/> says a row or a column was left out for the limit.
    /// </summary>
    internal static IReadOnlyList<IReadOnlyList<string>> Shape(IEnumerable<IReadOnlyList<string>> rows, out bool truncated)
    {
        var filled = rows
            .Where(row => row.Any(cell => !string.IsNullOrWhiteSpace(cell)))
            .Take(MaxRows + 2)
            .ToList();
        truncated = filled.Count > MaxRows + 1 || filled.Any(row => row.Count > MaxColumns);
        var kept = filled
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
