using System.Text.Json;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The content of an <see cref="OmniSpreadsheet"/>: rows of cell inputs, exactly as they were typed.
/// </summary>
/// <remarks>
/// A cell holds its input, never its result: <c>12</c>, <c>Loyer</c> or <c>=SUM(B2:B6)</c>. What a
/// cell shows is computed from the inputs by <see cref="Evaluate(int, int)"/>, so the sheet stores
/// and serialises nothing that could drift from them. The sheet is immutable: every change returns
/// a new sheet, which is what <see cref="OmniSpreadsheet.ValueChanged"/> raises.
/// <para>
/// <see cref="ToJson"/> writes <c>{"columnCount":3,"rows":[["Poste","Janvier"],...]}</c>, which
/// <see cref="FromJson"/> reads back; the type also serialises as is with <c>System.Text.Json</c>.
/// Positions are zero-based; addresses use the A1 notation (<c>A1</c> is row 0, column 0).
/// </para>
/// </remarks>
public sealed record OmniSpreadsheetData
{
    /// <summary>Number of columns. Rows shorter than this read as empty on their missing cells.</summary>
    public int ColumnCount { get; init; }

    /// <summary>The rows, each a list of cell inputs from column A onwards.</summary>
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = [];

    /// <summary>Number of rows.</summary>
    public int RowCount => Rows.Count;

    /// <summary>An empty sheet of the given size.</summary>
    public static OmniSpreadsheetData Create(int rowCount, int columnCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rowCount);
        ArgumentOutOfRangeException.ThrowIfNegative(columnCount);
        return new OmniSpreadsheetData
        {
            ColumnCount = columnCount,
            Rows = [.. Enumerable.Range(0, rowCount).Select(_ => EmptyRow(columnCount))]
        };
    }

    /// <summary>
    /// A sheet holding the given rows, at least <paramref name="rowCount"/> by
    /// <paramref name="columnCount"/>: the extra room is empty cells to type into.
    /// </summary>
    public static OmniSpreadsheetData FromRows(IEnumerable<IEnumerable<string?>> rows, int rowCount = 0, int columnCount = 0)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var read = rows.Select(row => (IReadOnlyList<string>)row.Select(cell => cell ?? string.Empty).ToArray()).ToList();
        var columns = Math.Max(columnCount, read.Count == 0 ? 0 : read.Max(row => row.Count));
        while (read.Count < rowCount)
        {
            read.Add([]);
        }

        return new OmniSpreadsheetData { ColumnCount = columns, Rows = [.. read.Select(row => Pad(row, columns))] };
    }

    /// <summary>The input of a cell, empty outside the sheet.</summary>
    public string GetInput(int row, int column) =>
        row >= 0 && row < Rows.Count && column >= 0 && column < ColumnCount && column < Rows[row].Count
            ? Rows[row][column] ?? string.Empty
            : string.Empty;

    /// <summary>The input of the cell at an A1 address, empty for an address outside the sheet.</summary>
    public string GetInput(string address) =>
        SpreadsheetAddress.TryParse(address, out var row, out var column) ? GetInput(row, column) : string.Empty;

    /// <summary>
    /// The sheet with one cell's input replaced. A position past the last row or column grows the
    /// sheet to reach it.
    /// </summary>
    public OmniSpreadsheetData WithInput(int row, int column, string? input)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(row);
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        var columns = Math.Max(ColumnCount, column + 1);
        var rows = Rows.Select(existing => Pad(existing, columns)).ToList();
        while (rows.Count <= row)
        {
            rows.Add(EmptyRow(columns));
        }

        var cells = rows[row].ToArray();
        cells[column] = input ?? string.Empty;
        rows[row] = cells;
        return this with { ColumnCount = columns, Rows = rows };
    }

    /// <summary>The sheet with the input of the cell at an A1 address replaced.</summary>
    /// <exception cref="ArgumentException">The address is not in the A1 notation.</exception>
    public OmniSpreadsheetData WithInput(string address, string? input) =>
        SpreadsheetAddress.TryParse(address, out var row, out var column)
            ? WithInput(row, column, input)
            : throw new ArgumentException($"'{address}' is not a cell address.", nameof(address));

    /// <summary>The sheet with an empty row added at the bottom.</summary>
    public OmniSpreadsheetData AddRow() => this with { Rows = [.. Rows, EmptyRow(ColumnCount)] };

    /// <summary>The sheet with an empty column added on the right.</summary>
    public OmniSpreadsheetData AddColumn() => this with
    {
        ColumnCount = ColumnCount + 1,
        Rows = [.. Rows.Select(row => Pad(row, ColumnCount + 1))]
    };

    /// <summary>
    /// What a cell shows: its number, its text, or its formula computed against the rest of the
    /// sheet. Computing one cell computes every cell it depends on.
    /// </summary>
    public OmniSpreadsheetValue Evaluate(int row, int column) => new SpreadsheetEvaluator(this).Evaluate(row, column);

    /// <summary>What the cell at an A1 address shows; empty for an address outside the sheet.</summary>
    public OmniSpreadsheetValue Evaluate(string address) =>
        SpreadsheetAddress.TryParse(address, out var row, out var column) ? Evaluate(row, column) : OmniSpreadsheetValue.Empty;

    /// <summary>The letters naming a zero-based column: 0 is A, 25 is Z, 26 is AA.</summary>
    public static string ColumnName(int column) => SpreadsheetAddress.ColumnName(column);

    /// <summary>The A1 address of a zero-based position, for instance <c>B3</c> for row 2, column 1.</summary>
    public static string Address(int row, int column) => SpreadsheetAddress.Format(row, column);

    /// <summary>Writes the sheet as compact JSON.</summary>
    public string ToJson()
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteNumber("columnCount", ColumnCount);
            writer.WriteStartArray("rows");
            foreach (var row in Rows)
            {
                writer.WriteStartArray();
                foreach (var cell in row)
                {
                    writer.WriteStringValue(cell ?? string.Empty);
                }

                writer.WriteEndArray();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>Reads a sheet written by <see cref="ToJson"/>, squaring off rows of uneven length.</summary>
    /// <exception cref="JsonException">The text is not a sheet.</exception>
    public static OmniSpreadsheetData FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("The text holds no spreadsheet.");
        }

        var columns = root.TryGetProperty("columnCount", out var count) && count.TryGetInt32(out var declared) ? Math.Max(0, declared) : 0;
        var rows = new List<IEnumerable<string?>>();
        if (root.TryGetProperty("rows", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in items.EnumerateArray())
            {
                rows.Add(row.ValueKind == JsonValueKind.Array
                    ? [.. row.EnumerateArray().Select(cell => cell.ValueKind == JsonValueKind.String ? cell.GetString() : cell.ToString())]
                    : []);
            }
        }

        return FromRows(rows, columnCount: columns);
    }

    private static IReadOnlyList<string> EmptyRow(int columns) => [.. Enumerable.Repeat(string.Empty, columns)];

    private static IReadOnlyList<string> Pad(IReadOnlyList<string> row, int columns) => row.Count >= columns
        ? row
        : [.. row, .. Enumerable.Repeat(string.Empty, columns - row.Count)];
}
