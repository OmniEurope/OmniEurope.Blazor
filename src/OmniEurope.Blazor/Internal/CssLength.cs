namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The one check of a CSS length that a component hands to its script as a custom property (a height, a
/// maximum height, a dialog width): nothing but a number and a unit, so no expression, no second
/// declaration and no url can reach the stylesheet.
/// </summary>
/// <remarks>
/// Grammar: <c>length = digits [ "." digits ] unit</c>, with ASCII digits only, no sign, no space, no
/// exponent, and a lowercase unit. A height takes <c>px</c>, <c>rem</c>, <c>em</c>, <c>vh</c> or <c>%</c>;
/// a width takes <c>px</c>, <c>rem</c>, <c>em</c>, <c>ch</c>, <c>vw</c> or <c>%</c>. A keyword such as
/// <c>auto</c> or <c>none</c> is not a length: the component that allows one tests it before calling here.
/// </remarks>
internal static class CssLength
{
    private static readonly string[] HeightUnits = ["px", "rem", "em", "vh", "%"];
    private static readonly string[] WidthUnits = ["px", "rem", "em", "ch", "vw", "%"];

    /// <summary>Whether <paramref name="value"/> is a number followed by px, rem, em, vh or %.</summary>
    internal static bool IsHeight(string? value) => Matches(value, HeightUnits, positive: false);

    /// <summary>
    /// Whether <paramref name="value"/> is a number followed by px, rem, em, ch, vw or %; with
    /// <paramref name="positive"/>, a number above zero (a width of zero shows nothing).
    /// </summary>
    internal static bool IsWidth(string? value, bool positive = false) => Matches(value, WidthUnits, positive);

    private static bool Matches(string? value, string[] units, bool positive)
    {
        if (value is null)
        {
            return false;
        }

        var index = 0;
        var nonZero = false;
        while (index < value.Length && char.IsAsciiDigit(value[index]))
        {
            nonZero |= value[index] != '0';
            index++;
        }

        if (index == 0)
        {
            return false;
        }

        if (index < value.Length && value[index] == '.')
        {
            var fraction = ++index;
            while (index < value.Length && char.IsAsciiDigit(value[index]))
            {
                nonZero |= value[index] != '0';
                index++;
            }

            if (index == fraction)
            {
                return false;
            }
        }

        if (positive && !nonZero)
        {
            return false;
        }

        var unitText = value.AsSpan(index);
        foreach (var unit in units)
        {
            if (unitText.SequenceEqual(unit))
            {
                return true;
            }
        }

        return false;
    }
}
