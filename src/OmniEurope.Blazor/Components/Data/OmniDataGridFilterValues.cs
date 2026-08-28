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

    public static string Join(IEnumerable<string> values) =>
        string.Join(Separator, values.Where(value => !string.IsNullOrEmpty(value)));

    public static IReadOnlyList<string> Split(string? value) => string.IsNullOrEmpty(value)
        ? []
        : value.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

    /// <summary>True when the operator reads its value as a list rather than as a single candidate.</summary>
    public static bool IsMultiValued(OmniDataGridFilterOperator candidate) =>
        candidate is OmniDataGridFilterOperator.In or OmniDataGridFilterOperator.NotIn;
}
