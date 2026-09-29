namespace OmniEurope.Blazor.Components;

/// <summary>
/// Encoding of a multi-valued filter. The filter value stays a single string everywhere it travels,
/// so neither the remote loader contract nor the persisted grid configuration had to grow a second
/// shape; the <see cref="OmniDataGridFilterOperator.In"/> operator is what says the string carries a
/// list, and these methods are the only place that knows how it is written.
/// </summary>
public static class OmniDataGridFilterValues
{
    /// <summary>ASCII unit separator: not typable, so it can never collide with a real value.</summary>
    private const char Separator = (char)0x1F;

    /// <summary>Encodes several candidates into one filter value, dropping the null or empty ones.</summary>
    /// <param name="values">The candidates, in order.</param>
    /// <returns>The encoded value; empty when no candidate is left.</returns>
    public static string Join(IEnumerable<string> values) =>
        string.Join(Separator, values.Where(value => !string.IsNullOrEmpty(value)));

    /// <summary>Decodes a filter value written by <see cref="Join"/> back into its candidates.</summary>
    /// <param name="value">The encoded value; a plain value without separator gives a single candidate.</param>
    /// <returns>The non-empty candidates; empty when <paramref name="value"/> is null or empty.</returns>
    public static IReadOnlyList<string> Split(string? value) => string.IsNullOrEmpty(value)
        ? []
        : value.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

    /// <summary>True when the operator reads its value as a list rather than as a single candidate.</summary>
    /// <param name="candidate">The operator to test.</param>
    /// <returns>True for <see cref="OmniDataGridFilterOperator.In"/> and <see cref="OmniDataGridFilterOperator.NotIn"/>.</returns>
    public static bool IsMultiValued(OmniDataGridFilterOperator candidate) =>
        candidate is OmniDataGridFilterOperator.In or OmniDataGridFilterOperator.NotIn;
}
