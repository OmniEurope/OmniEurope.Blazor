using System.Text.RegularExpressions;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The literal CSS values a mail may carry in its <c>style</c> attributes. Each check refuses anything that
/// could close the declaration or the attribute (<c>;</c>, quotes, braces, <c>url(</c>), so a value from a
/// theme or a host can be written into a mail as it is.
/// </summary>
internal static partial class MailCss
{
    /// <summary>Fonts every mail client has, after the theme's own.</summary>
    internal const string SafeFontStack = "Arial, Helvetica, sans-serif";

    private static readonly string[] GenericFamilies = ["serif", "sans-serif", "monospace", "cursive", "fantasy"];

    /// <summary>A hexadecimal colour, an <c>rgb()</c>, <c>rgba()</c>, <c>hsl()</c> or <c>hsla()</c> of numbers, or a keyword.</summary>
    internal static bool IsColor(string value) => ColorPattern().IsMatch(value.Trim());

    /// <summary>A list of family names, quoted or not.</summary>
    internal static bool IsFontFamily(string value) => FontPattern().IsMatch(value.Trim());

    /// <summary>The family list with its double quotes made single (the attribute is double-quoted) and the safe fonts at its end.</summary>
    internal static string WithSafeFallback(string family)
    {
        var trimmed = family.Trim().Replace('"', '\'');
        var last = trimmed.Split(',')[^1].Trim().Trim('\'');
        return GenericFamilies.Contains(last, StringComparer.OrdinalIgnoreCase) ? trimmed : $"{trimmed}, {SafeFontStack}";
    }

    /// <summary>A length in <c>px</c>, <c>rem</c> or <c>em</c> (or unitless 0) as whole pixels, 16 to the rem; null for anything else.</summary>
    internal static string? Pixels(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var match = LengthPattern().Match(value.Trim());
        if (!match.Success)
        {
            return null;
        }

        var number = double.Parse(match.Groups["number"].Value, CultureInfo.InvariantCulture);
        var pixels = match.Groups["unit"].Value is "rem" or "em" ? number * 16 : number;
        if (match.Groups["unit"].Value.Length == 0 && number != 0)
        {
            return null;
        }

        return Math.Round(pixels).ToString(CultureInfo.InvariantCulture) + "px";
    }

    /// <summary>Refuses a value of a theme the host set by hand that is not a literal colour.</summary>
    internal static string Color(string value, string property)
    {
        if (!IsColor(value))
        {
            throw new ArgumentException($"The mail theme's {property} must be a literal colour (#rrggbb, rgb(), hsl() or a keyword), not '{value}'.", nameof(value));
        }

        return value.Trim();
    }

    [GeneratedRegex(@"^(#[0-9a-fA-F]{3,4}|#[0-9a-fA-F]{6}|#[0-9a-fA-F]{8}|(rgb|rgba|hsl|hsla)\(\s*[0-9.%\s,/+-]+\)|[a-zA-Z]{3,20})$")]
    private static partial Regex ColorPattern();

    [GeneratedRegex(@"^[A-Za-z0-9 \-_""',]{1,200}$")]
    private static partial Regex FontPattern();

    [GeneratedRegex(@"^(?<number>[0-9]+(\.[0-9]+)?)(?<unit>px|rem|em)?$")]
    private static partial Regex LengthPattern();
}
