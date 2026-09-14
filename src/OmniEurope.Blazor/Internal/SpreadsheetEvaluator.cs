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
    private readonly OmniSpreadsheetData _sheet;
    private readonly Dictionary<(int Row, int Column), OmniSpreadsheetValue> _values = [];
    private readonly HashSet<(int Row, int Column)> _pending = [];

    internal SpreadsheetEvaluator(OmniSpreadsheetData sheet) => _sheet = sheet;

    internal bool Contains(int row, int column) =>
        row >= 0 && column >= 0 && row < _sheet.RowCount && column < _sheet.ColumnCount;

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

        var value = Read(_sheet.GetInput(row, column));
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
