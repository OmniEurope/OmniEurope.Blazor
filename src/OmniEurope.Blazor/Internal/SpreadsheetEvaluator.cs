using System.Globalization;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Computes the cells of one sheet. Each cell is computed once and remembered, so a formula read by
/// many others costs one evaluation; a cell met again while it is still being computed is a cycle
/// and reads <see cref="OmniSpreadsheetValue.CircularError"/>. One instance serves one immutable
/// sheet: a new sheet gets a new evaluator.
/// </summary>
internal sealed class SpreadsheetEvaluator
{
    private readonly IReadOnlyList<IReadOnlyList<string>> _rows;
    private readonly int _columnCount;
    private readonly Dictionary<(int Row, int Column), OmniSpreadsheetValue> _values = [];
    private readonly HashSet<(int Row, int Column)> _pending = [];

    /// <summary>
    /// How deep a computation may nest, counting brackets, signs and arguments within a formula and
    /// the chain of cells formulas read: far past any sheet written by hand, far below the stack.
    /// </summary>
    internal const int MaxDepth = 256;

    internal SpreadsheetEvaluator(IReadOnlyList<IReadOnlyList<string>> rows, int columnCount)
    {
        _rows = rows;
        _columnCount = columnCount;
    }

    /// <summary>The current nesting of the computation in progress, kept by <see cref="SpreadsheetFormula"/>.</summary>
    internal int Depth { get; set; }

    // Same reading as the sheet's own GetInput: a position outside the sheet or a short row is empty.
    private string InputOf(int row, int column) =>
        Contains(row, column) && column < _rows[row].Count ? _rows[row][column] ?? string.Empty : string.Empty;

    internal bool Contains(int row, int column) =>
        row >= 0 && column >= 0 && row < _rows.Count && column < _columnCount;

    internal OmniSpreadsheetValue Evaluate(int row, int column)
    {
        if (!Contains(row, column))
        {
            return OmniSpreadsheetValue.Empty;
        }

        var key = (row, column);
        if (_values.TryGetValue(key, out var known))
        {
            return known;
        }

        if (!_pending.Add(key))
        {
            return OmniSpreadsheetValue.FromError(OmniSpreadsheetValue.CircularError);
        }

        var value = Read(InputOf(row, column));
        _pending.Remove(key);
        _values[key] = value;
        return value;
    }

    /// <summary>
    /// What an input reads as: nothing, a formula after <c>=</c>, forced text after an apostrophe, a
    /// number (in the current culture or with a point, a trailing <c>%</c> dividing by a hundred), or
    /// text.
    /// </summary>
    private OmniSpreadsheetValue Read(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return OmniSpreadsheetValue.Empty;
        }

        if (input[0] == '=')
        {
            return SpreadsheetFormula.Evaluate(input[1..], this);
        }

        if (input[0] == '\'')
        {
            return OmniSpreadsheetValue.FromText(input[1..]);
        }

        return TryReadNumber(input, out var number)
            ? OmniSpreadsheetValue.FromNumber(number)
            : OmniSpreadsheetValue.FromText(input);
    }

    internal static bool TryReadNumber(string text, out double number)
    {
        var trimmed = text.Trim();
        var percent = trimmed.EndsWith('%');
        if (percent)
        {
            trimmed = trimmed[..^1].TrimEnd();
        }

        const NumberStyles styles = NumberStyles.Float;
        if (double.TryParse(trimmed, styles, CultureInfo.CurrentCulture, out number)
            || double.TryParse(trimmed, styles, CultureInfo.InvariantCulture, out number))
        {
            if (percent)
            {
                number /= 100d;
            }

            return true;
        }

        number = 0d;
        return false;
    }
}
