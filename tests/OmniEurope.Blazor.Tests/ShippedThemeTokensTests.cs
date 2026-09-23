using System.Text;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The shipped look is the theme <c>Défaut</c> painted with the palette <c>Défaut</c> (PLAN-008 D2,
/// lot 7). Its tokens are not written by hand in the stylesheet: they are the generator's output,
/// between two markers, so that no colour token can ever be left without a dark value again (the
/// cause of the hover turning near-white on a dark page).
/// </summary>
/// <remarks>
/// When the generator or the catalogue changes, regenerate the block by running this test with the
/// environment variable <c>OMNI_WRITE_THEME_TOKENS=1</c>, then review the diff. Without it, the test
/// only compares.
/// </remarks>
public sealed class ShippedThemeTokensTests
{
    internal const string StartMarker = "/* omni:theme-tokens:start */";
    internal const string EndMarker = "/* omni:theme-tokens:end */";

    private static string StylesheetPath => Path.Combine(RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot", "omnieurope.blazor.css");

    [Fact]
    public void The_stylesheet_carries_exactly_the_generated_tokens_of_the_default_theme()
    {
        var css = File.ReadAllText(StylesheetPath).ReplaceLineEndings("\n");
        var expected = Render();

        if (Environment.GetEnvironmentVariable("OMNI_WRITE_THEME_TOKENS") == "1")
        {
            File.WriteAllText(StylesheetPath, Replace(css, expected));
            css = File.ReadAllText(StylesheetPath).ReplaceLineEndings("\n");
        }

        Assert.Equal(expected, Extract(css));
    }

    [Fact]
    public void Every_generated_colour_token_has_a_light_and_a_dark_value()
    {
        var preset = Shipped();
        var colours = preset.Light.Keys.Where(key => !preset.Shape.ContainsKey(key)).ToArray();

        Assert.NotEmpty(colours);
        Assert.All(colours, key => Assert.True(preset.Dark.ContainsKey(key), $"{key} has no dark value."));
    }

    [Fact]
    public void A_hand_edit_inside_the_generated_block_is_detected()
    {
        var generated = Render();
        var edited = generated.Replace("--omni-color-accent: ", "--omni-color-accent: #123456; --omni-was: ", StringComparison.Ordinal);

        Assert.NotEqual(generated, edited);
        Assert.NotEqual(generated, Extract(Replace(File.ReadAllText(StylesheetPath).ReplaceLineEndings("\n"), edited)));
    }

    private static OmniThemePreset Shipped() => OmniThemePresets.All.Single(preset => preset.Name == "Essentiel");

    /// <summary>
    /// Light values on the root and on a light scope, dark values on a dark scope and on a system scope
    /// whose system is dark. The shape is declared on the root and again on every scope: several shape
    /// values name a mode token (the card background, the shadows), and a custom property resolves the
    /// var() it names where it is declared, so declared on the root alone a dark scope would inherit
    /// the light values.
    /// </summary>
    internal static string Render()
    {
        var preset = Shipped();
        var colourNames = preset.Light.Keys.Where(key => !preset.Shape.ContainsKey(key)).Order(StringComparer.Ordinal).ToArray();
        var builder = new StringBuilder();
        builder.Append(StartMarker).Append('\n');
        builder.Append("/* Generated from the theme and the palette Défaut by ShippedThemeTokensTests: do not edit by hand. */\n");
        Block(builder, ":root,\n[data-omni-theme=\"light\"]", colourNames.Select(name => (name, preset.Light[name])), string.Empty, null);
        Block(builder, ":root,\n[data-omni-theme]", preset.Shape.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => (pair.Key, pair.Value)), string.Empty, null);
        Block(builder, "[data-omni-theme=\"dark\"]", colourNames.Select(name => (name, preset.Dark[name])), string.Empty, "dark");
        builder.Append("@media (prefers-color-scheme: dark) {\n");
        Block(builder, "    [data-omni-theme=\"system\"]", colourNames.Select(name => (name, preset.Dark[name])), "    ", "dark");
        builder.Append("}\n");
        builder.Append(EndMarker);
        return builder.ToString();
    }

    private static void Block(StringBuilder builder, string selector, IEnumerable<(string Name, string Value)> tokens, string indent, string? colorScheme)
    {
        builder.Append(selector).Append(" {\n");
        foreach (var (name, value) in tokens)
        {
            builder.Append(indent).Append("    ").Append(name).Append(": ").Append(value).Append(";\n");
        }

        if (colorScheme is not null)
        {
            builder.Append(indent).Append("    color-scheme: ").Append(colorScheme).Append(";\n");
        }

        builder.Append(indent).Append("}\n");
    }

    private static string Extract(string css)
    {
        var start = css.IndexOf(StartMarker, StringComparison.Ordinal);
        var end = css.IndexOf(EndMarker, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start, "The stylesheet has no generated theme token block.");
        return css[start..(end + EndMarker.Length)];
    }

    private static string Replace(string css, string block)
    {
        var start = css.IndexOf(StartMarker, StringComparison.Ordinal);
        var end = css.IndexOf(EndMarker, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start, "The stylesheet has no generated theme token block.");
        return string.Concat(css.AsSpan(0, start), block, css.AsSpan(end + EndMarker.Length));
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OmniEurope.Blazor.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }
}
