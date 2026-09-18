using System.Globalization;
using System.Text.RegularExpressions;

namespace OmniEurope.Blazor.Showcase.Theming;

/// <summary>
/// The contrast ratios of the combination the visitor is previewing, pair by pair, as the reference
/// mockup lists them next to its preview (<c>plans/PLAN-008-maquette-themes.html</c>, <c>PAIRS</c>).
/// </summary>
/// <remarks>
/// The ratios are computed from the token values the page receives, with the WCAG 2 relative
/// luminance. A value this reader cannot resolve to an opaque colour (a colour with transparency, a
/// form it does not know) gives no ratio rather than a guessed one.
/// </remarks>
public static partial class ContrastAudit
{
    /// <summary>
    /// The pairs of the mockup, in its order: text on the surfaces, the accents, every ink on its
    /// fill (hover and press included), then the fills and the border against the surface. The last
    /// threshold is the 1.7 floor of the package, argued in
    /// <c>ShowcaseThemeTests.EveryPalette_KeepsItsBordersVisible</c>.
    /// </summary>
    public static IReadOnlyList<ContrastPair> Pairs { get; } =
    [
        new("ContrastPair01", "--omni-color-text", "--omni-color-surface", 4.5),
        new("ContrastPair02", "--omni-color-text", "--omni-color-surface-muted", 4.5),
        new("ContrastPair03", "--omni-color-text", "--omni-color-surface-hover", 4.5),
        new("ContrastPair04", "--omni-color-text-muted", "--omni-color-surface", 4.5),
        new("ContrastPair05", "--omni-color-text-muted", "--omni-color-surface-muted", 4.5),
        new("ContrastPair06", "--omni-color-text-muted", "--omni-color-surface-hover", 4.5),
        new("ContrastPair07", "--omni-color-accent-strong", "--omni-color-surface", 4.5),
        new("ContrastPair08", "--omni-color-accent-strong", "--omni-color-accent-subtle", 4.5),
        new("ContrastPair09", "--omni-color-accent-strong", "--omni-color-surface-hover", 4.5),
        new("ContrastPair10", "--omni-color-on-accent", "--omni-color-accent", 4.5),
        new("ContrastPair11", "--omni-color-on-success", "--omni-color-success", 4.5),
        new("ContrastPair12", "--omni-color-on-warning", "--omni-color-warning", 4.5),
        new("ContrastPair13", "--omni-color-on-danger", "--omni-color-danger", 4.5),
        new("ContrastPair14", "--omni-color-on-inverse", "--omni-color-inverse-surface", 4.5),
        new("ContrastPair15", "--omni-color-success", "--omni-color-surface", 4.5),
        new("ContrastPair16", "--omni-color-success", "--omni-color-success-subtle", 4.5),
        new("ContrastPair17", "--omni-color-warning", "--omni-color-surface", 4.5),
        new("ContrastPair18", "--omni-color-danger", "--omni-color-surface", 4.5),
        new("ContrastPair19", "--omni-color-danger", "--omni-color-danger-subtle", 4.5),
        new("ContrastPair20", "--omni-color-on-accent-fill", "--omni-color-accent-fill", 4.5),
        new("ContrastPair21", "--omni-color-on-success-fill", "--omni-color-success-fill", 4.5),
        new("ContrastPair22", "--omni-color-on-info-fill", "--omni-color-info-fill", 4.5),
        new("ContrastPair23", "--omni-color-on-bright", "--omni-color-info-bright", 4.5),
        new("ContrastPair24", "--omni-color-on-bright", "--omni-color-success-bright", 4.5),
        new("ContrastPair25", "--omni-color-on-bright", "--omni-color-info-bright-hover", 4.5),
        new("ContrastPair26", "--omni-color-on-bright", "--omni-color-info-bright-active", 4.5),
        new("ContrastPair27", "--omni-color-on-bright", "--omni-color-success-bright-hover", 4.5),
        new("ContrastPair28", "--omni-color-on-bright", "--omni-color-success-bright-active", 4.5),
        new("ContrastPair29", "--omni-color-on-deep", "--omni-color-warning-deep", 4.5),
        new("ContrastPair30", "--omni-color-on-deep", "--omni-color-warning-deep-hover", 4.5),
        new("ContrastPair31", "--omni-color-on-deep", "--omni-color-warning-deep-active", 4.5),
        new("ContrastPair32", "--omni-color-on-deep", "--omni-color-danger-deep", 4.5),
        new("ContrastPair33", "--omni-color-on-deep", "--omni-color-danger-deep-hover", 4.5),
        new("ContrastPair34", "--omni-color-on-deep", "--omni-color-danger-deep-active", 4.5),
        new("ContrastPair35", "--omni-color-on-accent-fill", "--omni-color-accent-fill-hover", 4.5),
        new("ContrastPair36", "--omni-color-on-accent-fill", "--omni-color-accent-fill-active", 4.5),
        new("ContrastPair37", "--omni-color-text", "--omni-color-neutral-fill", 4.5),
        new("ContrastPair38", "--omni-color-text", "--omni-color-neutral-fill-hover", 4.5),
        new("ContrastPair39", "--omni-color-text", "--omni-color-neutral-fill-active", 4.5),
        new("ContrastPair40", "--omni-color-accent-strong", "--omni-color-surface-highlight", 4.5),
        new("ContrastPair41", "--omni-color-accent-fill", "--omni-color-surface", 3.0),
        new("ContrastPair42", "--omni-color-success-fill", "--omni-color-surface", 3.0),
        new("ContrastPair43", "--omni-color-info-fill", "--omni-color-surface", 3.0),
        new("ContrastPair44", "--omni-color-warning-fill", "--omni-color-surface", 3.0),
        new("ContrastPair45", "--omni-color-danger-fill", "--omni-color-surface", 3.0),
        new("ContrastPair46", "--omni-color-accent", "--omni-color-surface", 3.0),
        new("ContrastPair47", "--omni-color-border", "--omni-color-surface", 1.7)
    ];

    /// <summary>The colour tokens the mockup shows as swatches beside the ratios.</summary>
    public static IReadOnlyList<string> Swatches { get; } =
    [
        "--omni-color-accent-fill", "--omni-color-success-fill",
        "--omni-color-info-fill", "--omni-color-warning-fill",
        "--omni-color-danger-fill", "--omni-color-accent",
        "--omni-color-accent-strong", "--omni-color-success",
        "--omni-color-info", "--omni-color-warning",
        "--omni-color-danger", "--omni-color-text",
        "--omni-color-text-muted", "--omni-color-border",
        "--omni-color-surface", "--omni-color-surface-muted",
        "--omni-color-surface-hover", "--omni-color-surface-highlight"
    ];

    /// <summary>
    /// Every pair measured against one half of the combination. A token the half does not set takes
    /// its shipped value from the catalogue, as the page does.
    /// </summary>
    public static IReadOnlyList<ContrastResult> Measure(IReadOnlyDictionary<string, string> half, IReadOnlyList<ThemeToken> catalogue)
    {
        ArgumentNullException.ThrowIfNull(half);
        ArgumentNullException.ThrowIfNull(catalogue);
        var tokens = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var token in catalogue)
        {
            tokens[token.Name] = token.DefaultValue;
        }

        foreach (var (name, value) in half)
        {
            tokens[name] = value;
        }

        return
        [
            .. Pairs.Select(pair =>
            {
                var foreground = Resolve(tokens, pair.Foreground, 0);
                var background = Resolve(tokens, pair.Background, 0);
                return new ContrastResult(pair, foreground is null || background is null ? null : Ratio(foreground.Value, background.Value));
            })
        ];
    }

    /// <summary>The WCAG 2 contrast ratio of two opaque colours.</summary>
    public static double Ratio((double R, double G, double B) first, (double R, double G, double B) second)
    {
        var a = Luminance(first);
        var b = Luminance(second);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    /// <summary>The value of a token as an opaque colour, or null when it cannot be resolved.</summary>
    public static (double R, double G, double B)? ResolveToken(IReadOnlyDictionary<string, string> tokens, string name)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        return Resolve(tokens, name, 0);
    }

    private static (double R, double G, double B)? Resolve(IReadOnlyDictionary<string, string> tokens, string name, int depth) =>
        depth < 16 && tokens.TryGetValue(name, out var value) ? Parse(tokens, value.Trim(), depth + 1) : null;

    private static (double R, double G, double B)? Parse(IReadOnlyDictionary<string, string> tokens, string value, int depth)
    {
        if (Hex().Match(value) is { Success: true } hex)
        {
            var digits = hex.Groups["digits"].Value;
            if (digits.Length == 3)
            {
                digits = string.Concat(digits.Select(digit => new string(digit, 2)));
            }

            return (
                int.Parse(digits[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                int.Parse(digits[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                int.Parse(digits[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
        }

        if (Reference().Match(value) is { Success: true } reference)
        {
            return Resolve(tokens, reference.Groups["name"].Value, depth);
        }

        if (Mix().Match(value) is { Success: true } mix)
        {
            var first = Parse(tokens, mix.Groups["first"].Value, depth + 1);
            var second = Parse(tokens, mix.Groups["second"].Value, depth + 1);
            if (first is null || second is null)
            {
                return null;
            }

            var share = double.Parse(mix.Groups["share"].Value, CultureInfo.InvariantCulture) / 100d;
            return (
                (first.Value.R * share) + (second.Value.R * (1 - share)),
                (first.Value.G * share) + (second.Value.G * (1 - share)),
                (first.Value.B * share) + (second.Value.B * (1 - share)));
        }

        return null;
    }

    private static double Luminance((double R, double G, double B) colour) =>
        (0.2126 * Channel(colour.R)) + (0.7152 * Channel(colour.G)) + (0.0722 * Channel(colour.B));

    private static double Channel(double value)
    {
        var channel = value / 255d;
        return channel <= 0.03928 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }

    [GeneratedRegex("^#(?<digits>[0-9a-fA-F]{6}|[0-9a-fA-F]{3})$")]
    private static partial Regex Hex();

    [GeneratedRegex(@"^var\((?<name>--omni-[a-z0-9-]+)\)$")]
    private static partial Regex Reference();

    [GeneratedRegex(@"^color-mix\(in srgb, (?<first>[^,]+?) (?<share>\d+(?:\.\d+)?)%, (?<second>[^,]+)\)$")]
    private static partial Regex Mix();
}

/// <summary>One pair of the audit: a foreground token read on a background token, and the ratio it must reach.</summary>
/// <param name="LabelKey">Resource key of the pair's name.</param>
/// <param name="Foreground">The token drawn on top.</param>
/// <param name="Background">The token under it.</param>
/// <param name="Minimum">The ratio the pair must reach.</param>
public sealed record ContrastPair(string LabelKey, string Foreground, string Background, double Minimum);

/// <summary>A pair measured, with no ratio when a value could not be resolved to an opaque colour.</summary>
/// <param name="Pair">The pair measured.</param>
/// <param name="Ratio">The contrast ratio, or null.</param>
public sealed record ContrastResult(ContrastPair Pair, double? Ratio)
{
    /// <summary>Whether the pair reaches its minimum; false when it could not be measured.</summary>
    public bool Passes => Ratio >= Pair.Minimum;
}
