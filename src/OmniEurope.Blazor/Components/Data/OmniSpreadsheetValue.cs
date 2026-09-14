using System.Globalization;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The value of one spreadsheet cell: what its input reads as, or what its formula computes to.
/// </summary>
/// <remarks>
/// The error codes are the ones spreadsheet users already know: <c>#REF!</c> for a reference outside
/// the sheet, <c>#DIV/0!</c> for a division by zero, <c>#NAME?</c> for an unknown function,
/// <c>#VALUE!</c> for text where a number is expected, <c>#NUM!</c> for a result too large to hold,
/// <c>#CIRC!</c> for a formula that depends on itself and <c>#ERROR!</c> for a formula that cannot be
/// read.
/// </remarks>
public sealed record OmniSpreadsheetValue
{
    /// <summary>A reference to a cell outside the sheet.</summary>
    public const string ReferenceError = "#REF!";

    /// <summary>A division by zero, or an average of nothing.</summary>
    public const string DivisionByZeroError = "#DIV/0!";

    /// <summary>A function the sheet does not know.</summary>
    public const string NameError = "#NAME?";

    /// <summary>Text, or a range, where a single number is expected.</summary>
    public const string ValueError = "#VALUE!";

    /// <summary>A result that is not a finite number.</summary>
    public const string NumberError = "#NUM!";

    /// <summary>A formula that reaches back to its own cell.</summary>
    public const string CircularError = "#CIRC!";

    /// <summary>A formula that cannot be read.</summary>
    public const string SyntaxError = "#ERROR!";

    private OmniSpreadsheetValue(OmniSpreadsheetValueKind kind, double number, string text)
    {
        Kind = kind;
        Number = number;
        Text = text;
    }

    /// <summary>An empty cell.</summary>
    public static OmniSpreadsheetValue Empty { get; } = new(OmniSpreadsheetValueKind.Empty, 0d, string.Empty);

    public OmniSpreadsheetValueKind Kind { get; }

    /// <summary>The number, when <see cref="Kind"/> is <see cref="OmniSpreadsheetValueKind.Number"/>; zero otherwise.</summary>
    public double Number { get; }

    /// <summary>The text, or the error code; empty for a number or an empty cell.</summary>
    public string Text { get; }

    public bool IsError => Kind == OmniSpreadsheetValueKind.Error;

    public static OmniSpreadsheetValue FromNumber(double number) => double.IsFinite(number)
        ? new(OmniSpreadsheetValueKind.Number, number, string.Empty)
        : FromError(NumberError);

    public static OmniSpreadsheetValue FromText(string text) => new(OmniSpreadsheetValueKind.Text, 0d, text ?? string.Empty);

    public static OmniSpreadsheetValue FromError(string code) => new(OmniSpreadsheetValueKind.Error, 0d, code);

    /// <summary>
    /// What the cell shows: a number in the given culture, grouped by thousands and with up to ten
    /// decimals, the text as typed, or the error code.
    /// </summary>
    public string ToDisplayString(IFormatProvider? provider = null) => Kind switch
    {
        OmniSpreadsheetValueKind.Number => Number.ToString("#,##0.##########", provider ?? CultureInfo.CurrentCulture),
        OmniSpreadsheetValueKind.Empty => string.Empty,
        _ => Text
    };
}
