using System.Globalization;

namespace OmniEurope.Blazor.Showcase.Theming;

/// <summary>
/// The colour arithmetic the palette generator needs.
/// </summary>
/// <remarks>
/// Kept deliberately small and explicit: every derived value in the catalogue comes from one of
/// these operations, so a palette can be checked by reading the rule that produced it rather than
/// by trusting a table of hand-picked values.
/// </remarks>
public static class ThemeColor
{
    /// <summary>Parses <c>#rgb</c> or <c>#rrggbb</c> into its three channels.</summary>
    public static (int R, int G, int B) Parse(string hex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hex);
        var digits = hex.TrimStart('#');
        if (digits.Length == 3)
        {
            digits = string.Concat(digits.Select(character => new string(character, 2)));
        }

        if (digits.Length != 6)
        {
            throw new FormatException($"Unsupported colour literal: {hex}");
        }

        return (
            int.Parse(digits[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            int.Parse(digits[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            int.Parse(digits[4..], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }

    /// <summary>Formats three channels back into a lowercase <c>#rrggbb</c> literal.</summary>
    public static string ToHex(int r, int g, int b) =>
        string.Create(CultureInfo.InvariantCulture, $"#{Clamp(r):x2}{Clamp(g):x2}{Clamp(b):x2}");

    /// <summary>
    /// The WCAG relative luminance, used both to pick readable text and to decide whether an
    /// upstream palette is a light or a dark one.
    /// </summary>
    public static double Luminance(string hex)
    {
        var (r, g, b) = Parse(hex);
        return 0.2126 * Channel(r) + 0.7152 * Channel(g) + 0.0722 * Channel(b);

        static double Channel(int value)
        {
            var normalized = value / 255d;
            return normalized <= 0.03928 ? normalized / 12.92 : Math.Pow((normalized + 0.055) / 1.055, 2.4);
        }
    }

    /// <summary>The WCAG contrast ratio between two colours, from 1 to 21.</summary>
    public static double Contrast(string first, string second)
    {
        var a = Luminance(first);
        var b = Luminance(second);
        var (lighter, darker) = a >= b ? (a, b) : (b, a);
        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>Blends two colours, <paramref name="weight"/> being the share of the first.</summary>
    public static string Mix(string first, string second, double weight)
    {
        var (r1, g1, b1) = Parse(first);
        var (r2, g2, b2) = Parse(second);
        var share = Math.Clamp(weight, 0, 1);
        return ToHex(
            (int)Math.Round((r1 * share) + (r2 * (1 - share))),
            (int)Math.Round((g1 * share) + (g2 * (1 - share))),
            (int)Math.Round((b1 * share) + (b2 * (1 - share))));
    }

    /// <summary>Moves a colour towards black by the given amount.</summary>
    public static string Darken(string hex, double amount) => Mix("#000000", hex, Math.Clamp(amount, 0, 1));

    /// <summary>Moves a colour towards white by the given amount.</summary>
    public static string Lighten(string hex, double amount) => Mix("#ffffff", hex, Math.Clamp(amount, 0, 1));

    /// <summary>
    /// Black or white, whichever reads better on the given background. Used for text laid over a
    /// filled surface, where a fixed white breaks the moment the accent is a pale one.
    /// </summary>
    public static string ReadableOn(string background) =>
        Contrast(background, "#ffffff") >= Contrast(background, "#111111") ? "#ffffff" : "#111111";

    /// <summary>Whether the colour is dark enough that a palette built on it reads as a dark one.</summary>
    public static bool IsDark(string hex) => Luminance(hex) < 0.2;

    private static int Clamp(int value) => Math.Clamp(value, 0, 255);
}
