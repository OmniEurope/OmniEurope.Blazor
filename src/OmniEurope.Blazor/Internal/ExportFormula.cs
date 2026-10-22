namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The rule every export format shares to keep a text from running as a spreadsheet formula: a text that
/// starts with <c>=</c>, <c>+</c>, <c>-</c>, <c>@</c>, a tab or a carriage return is treated as one.
/// </summary>
internal static class ExportFormula
{
    private static readonly char[] Triggers = ['=', '+', '-', '@', '\t', '\r'];

    /// <summary>Whether a spreadsheet would read <paramref name="text"/> as a formula.</summary>
    internal static bool StartsLikeFormula(string text) =>
        text.Length > 0 && text.IndexOfAny(Triggers, 0, 1) == 0;
}
