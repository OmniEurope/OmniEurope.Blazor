using System.Text.RegularExpressions;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The checks <see cref="Components.OmniMap"/> makes on what a host gives it: the colour of a line, written as an SVG
/// <c>stroke</c> attribute only when it is a literal colour, and the address template of the tiles.
/// </summary>
internal static partial class MapInput
{
    private const string Number = @"[+-]?(?:\d+(?:\.\d+)?|\.\d+)";

    /// <summary>A hexadecimal colour (3, 4, 6 or 8 digits) or an <c>rgb()</c>, <c>rgba()</c>, <c>hsl()</c> or <c>hsla()</c> of numbers, commas or spaces with an optional <c>/ alpha</c>.</summary>
    internal static bool IsColor(string? value) => value is { Length: > 0 and <= 80 } && ColorPattern().IsMatch(value.Trim());

    /// <summary>
    /// Refuses a tile address template that is not an absolute <c>https://</c> or <c>http://</c> address, or a path rooted
    /// in <c>/</c>, or that lacks one of the <c>{z}</c>, <c>{x}</c> and <c>{y}</c> placeholders, or holds any other.
    /// </summary>
    /// <exception cref="ArgumentException">The template is not valid.</exception>
    internal static string TileUrl(string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            throw new ArgumentException("OmniMap.TileUrl must be an address template with {z}, {x} and {y}.", nameof(template));
        }

        var trimmed = template.Trim();
        var rooted = trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || (trimmed.StartsWith('/') && !trimmed.StartsWith("//", StringComparison.Ordinal));
        if (!rooted || trimmed.Any(char.IsWhiteSpace) || trimmed.IndexOfAny(['"', '\'', '<', '>', '\\']) >= 0)
        {
            throw new ArgumentException($"OmniMap.TileUrl must start with https://, http:// or a single /, without spaces or quotes: '{template}'.", nameof(template));
        }

        var placeholders = PlaceholderPattern().Matches(trimmed).Select(match => match.Value).ToArray();
        var unknown = placeholders.Where(name => name is not ("{z}" or "{x}" or "{y}")).ToArray();
        var missing = new[] { "{z}", "{x}", "{y}" }.Where(name => !placeholders.Contains(name)).ToArray();
        if (unknown.Length > 0 || missing.Length > 0 || trimmed.Count(c => c == '{') != placeholders.Length || trimmed.Count(c => c == '}') != placeholders.Length)
        {
            throw new ArgumentException($"OmniMap.TileUrl must hold exactly the placeholders {{z}}, {{x}} and {{y}}: '{template}'.", nameof(template));
        }

        return trimmed;
    }

    [GeneratedRegex("^(?:#[0-9a-fA-F]{3,4}|#[0-9a-fA-F]{6}|#[0-9a-fA-F]{8}"
        + @"|(?:rgba?|hsla?)\(\s*" + Number + @"(?:%|deg)?\s*,\s*" + Number + @"%?\s*,\s*" + Number + @"%?\s*(?:,\s*" + Number + @"%?\s*)?\)"
        + @"|(?:rgba?|hsla?)\(\s*" + Number + @"(?:%|deg)?\s+" + Number + @"%?\s+" + Number + @"%?\s*(?:/\s*" + Number + @"%?\s*)?\))$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ColorPattern();

    [GeneratedRegex(@"\{[^{}]*\}", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderPattern();
}
