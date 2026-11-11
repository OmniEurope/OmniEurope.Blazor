using System.Text.RegularExpressions;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Resolves the colour values of a theme token set to sRGB hex: shared by the showcase theme tests and the
/// library contrast tests, so it compiles on every target framework of the suite (the showcase tests run on
/// net11.0 only).
/// </summary>
internal static class ThemeTokenResolution
{
    /// <summary>
    /// Resolves the two forms a theme uses for a colour it derives: a token reference and a
    /// <c>color-mix</c> of two of them in sRGB. Anything else fails the test rather than being
    /// guessed, so a new form has to be taught here before it can ship.
    /// </summary>
    internal static string Resolve(IReadOnlyDictionary<string, string> tokens, string value)
    {
        if (Regex.IsMatch(value, "^#[0-9a-f]{6}$", RegexOptions.IgnoreCase))
        {
            return value;
        }

        var reference = Regex.Match(value, @"^var\((?<name>--omni-[a-z0-9-]+)\)$");
        if (reference.Success)
        {
            return Resolve(tokens, tokens[reference.Groups["name"].Value]);
        }

        var mix = Regex.Match(value, @"^color-mix\(in srgb, (?<first>var\(--omni-[a-z0-9-]+\)) (?<share>\d+)%, (?<second>var\(--omni-[a-z0-9-]+\)|transparent)\)$");
        Assert.True(mix.Success, $"Unsupported colour form: {value}");
        var share = int.Parse(mix.Groups["share"].Value, System.Globalization.CultureInfo.InvariantCulture) / 100d;

        // A colour mixed with transparent is that colour at the share's opacity, measured where it is
        // laid: over the page surface (a theme's backdrop is measured separately, see
        // ThemeContrastMatrixTests.Text_reads_on_every_stop_of_a_theme_backdrop).
        var second = mix.Groups["second"].Value == "transparent" ? tokens["--omni-color-surface"] : Resolve(tokens, mix.Groups["second"].Value);
        return ThemeColor.Mix(Resolve(tokens, mix.Groups["first"].Value), second, share);
    }

    /// <summary>
    /// A colour value laid over <paramref name="ground"/> instead of the page surface: the same as
    /// <see cref="Resolve"/> for an opaque value, and for a colour mixed with transparent, that colour at
    /// its opacity over the given ground.
    /// </summary>
    internal static string ResolveOver(IReadOnlyDictionary<string, string> tokens, string value, string ground)
    {
        var mix = Regex.Match(value, @"^color-mix\(in srgb, (?<first>var\(--omni-[a-z0-9-]+\)) (?<share>\d+)%, transparent\)$");
        if (!mix.Success)
        {
            return Resolve(tokens, value);
        }

        var share = int.Parse(mix.Groups["share"].Value, System.Globalization.CultureInfo.InvariantCulture) / 100d;
        return ThemeColor.Mix(Resolve(tokens, mix.Groups["first"].Value), ground, share);
    }
}
