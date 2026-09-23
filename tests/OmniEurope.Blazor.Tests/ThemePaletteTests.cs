using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Themes decide the shape, palettes the colours, and any palette paints any theme (PLAN-008 lots 4
/// to 6). The scope sends the combination to its script only when one of the two changes.
/// </summary>
public sealed class ThemePaletteTests : OmniBunitContext
{
    private const string ThemeModule = "./_content/OmniEurope.Blazor/omni-theme.js";

    [Fact]
    public void A_palette_alone_sends_its_colours_and_no_shape()
    {
        var module = JSInterop.SetupModule(ThemeModule);
        var palette = OmniThemePalettes.All.Single(entry => entry.Name == "Lagune");

        Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Palette, palette)
            .AddChildContent("Contenu"));

        var invocation = Assert.Single(module.Invocations["apply"]);
        Assert.Same(palette.Light, invocation.Arguments[1]);
        Assert.Same(palette.Dark, invocation.Arguments[2]);
        Assert.False(((IReadOnlyDictionary<string, string>)invocation.Arguments[1]!).ContainsKey("--omni-radius"));
    }

    [Fact]
    public void A_theme_and_a_foreign_palette_send_the_palette_colours_under_the_theme_shape()
    {
        var module = JSInterop.SetupModule(ThemeModule);
        var halo = OmniThemePresets.All.Single(entry => entry.Name == "Halo");
        var braise = OmniThemePalettes.All.Single(entry => entry.Name == "Braise");

        Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Preset, halo)
            .Add(component => component.Palette, braise)
            .AddChildContent("Contenu"));

        var invocation = Assert.Single(module.Invocations["apply"]);
        var light = (IReadOnlyDictionary<string, string>)invocation.Arguments[1]!;
        var dark = (IReadOnlyDictionary<string, string>)invocation.Arguments[2]!;
        Assert.Equal(braise.Light["--omni-color-accent"], light["--omni-color-accent"]);
        Assert.Equal(braise.Dark["--omni-color-accent"], dark["--omni-color-accent"]);
        Assert.Equal(halo.Shape["--omni-button-radius"], light["--omni-button-radius"]);
        Assert.Equal(halo.DarkShape["--omni-card-background"], dark["--omni-card-background"]);
        Assert.NotEqual(halo.DarkShape["--omni-card-background"], light.GetValueOrDefault("--omni-card-background"));
    }

    [Fact]
    public void Changing_only_the_palette_repaints_and_dropping_it_returns_to_the_theme_palette()
    {
        var module = JSInterop.SetupModule(ThemeModule);
        var galet = OmniThemePresets.All.Single(entry => entry.Name == "Galet");
        var prune = OmniThemePalettes.All.Single(entry => entry.Name == "Prune");
        var scope = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Preset, galet)
            .AddChildContent("Contenu"));

        scope.Render(parameters => parameters.Add(component => component.Palette, prune));
        Assert.Equal(2, module.Invocations["apply"].Count);
        var repainted = (IReadOnlyDictionary<string, string>)module.Invocations["apply"][1].Arguments[1]!;
        Assert.Equal(prune.Light["--omni-color-accent"], repainted["--omni-color-accent"]);

        scope.Render(parameters => parameters.Add(component => component.Palette, (OmniThemePalette?)null));
        Assert.Equal(3, module.Invocations["apply"].Count);
        Assert.Same(galet.Light, module.Invocations["apply"][2].Arguments[1]);
    }

    [Fact]
    public void A_scope_without_theme_nor_palette_never_loads_the_script()
    {
        Render<OmniThemeScope>(parameters => parameters.AddChildContent("Contenu"));

        Assert.DoesNotContain(JSInterop.Invocations, invocation => invocation.Identifier == "import");
    }

    [Fact]
    public void With_lays_the_shape_over_both_halves_and_the_dark_shape_over_the_dark_one()
    {
        var papier = OmniThemePresets.All.Single(entry => entry.Name == "Papier");
        var ocean = OmniThemePalettes.All.Single(entry => entry.Name == "Océan");

        var painted = papier.With(ocean);

        Assert.Equal("Papier", painted.Name);
        Assert.Equal("var(--omni-color-text)", painted.Light["--omni-color-border"]);
        Assert.Equal(papier.DarkShape["--omni-color-border"], painted.Dark["--omni-color-border"]);
        Assert.Equal(ocean.Light["--omni-color-surface"], painted.Light["--omni-color-surface"]);
        Assert.Equal(ocean.Dark["--omni-color-surface"], painted.Dark["--omni-color-surface"]);
    }

    /// <summary>
    /// PLAN-008 T23: the dark shape reaches the dark half only. Galet, Halo, Papier and Nénuphar give
    /// their cards other tokens in dark mode than in light mode; the six other themes give the same.
    /// </summary>
    [Fact]
    public void Only_the_four_themes_with_a_dark_shape_change_their_card_tokens_in_dark_mode()
    {
        string[] withDarkCards = ["Galet", "Halo", "Papier", "Nénuphar"];

        Assert.Equal(10, OmniThemePresets.All.Count);
        foreach (var preset in OmniThemePresets.All)
        {
            var cardTokens = preset.Light.Keys.Concat(preset.Dark.Keys)
                .Where(key => key.StartsWith("--omni-card-", StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            Assert.NotEmpty(cardTokens);
            var differs = cardTokens.Any(key => preset.Light.GetValueOrDefault(key) != preset.Dark.GetValueOrDefault(key));

            Assert.True(differs == withDarkCards.Contains(preset.Name), $"{preset.Name}: card tokens differ between light and dark = {differs}.");
        }
    }

    /// <summary>
    /// PLAN-008 T5: Rétro's hard shadow starts with a ring of the surface colour, so a button filled
    /// with the text colour does not merge with its offset shadow of the same colour.
    /// </summary>
    [Fact]
    public void Retro_button_shadow_rings_the_surface_before_its_offset_shadow()
    {
        var retro = OmniThemePresets.All.Single(entry => entry.Name == "Rétro");

        Assert.StartsWith("0 0 0 2px var(--omni-color-surface),", retro.Shape["--omni-button-shadow"], StringComparison.Ordinal);
    }

    /// <summary>
    /// PLAN-008 T9: each of the ten themes presses its buttons its own way. The stylesheet's defaults
    /// (<c>translateY(1px)</c>, and the button's own shadow) stand in for a token a theme omits.
    /// </summary>
    [Fact]
    public void The_ten_themes_give_ten_distinct_press_couples()
    {
        var couples = OmniThemePresets.All
            .Select(preset => (
                Transform: preset.Light.GetValueOrDefault("--omni-button-press-transform", "translateY(1px)"),
                Shadow: preset.Light.GetValueOrDefault("--omni-button-press-shadow") ?? preset.Light.GetValueOrDefault("--omni-button-shadow", string.Empty)))
            .ToArray();

        Assert.Equal(10, couples.Length);
        Assert.Equal(10, couples.Distinct().Count());
    }

    /// <summary>
    /// The values the reference mockup produces for the palette <c>Défaut</c> in light mode. A
    /// difference means the generator was not ported faithfully (PLAN-008 lot 4 control).
    /// </summary>
    [Theory]
    // The accent fill is one colour for both modes since Aetheus recette R-053 (decided 2026-09-21):
    // #4340d2 lightened until it clears 3:1 on the dark surface too, so it departs from the mockup.
    [InlineData("--omni-color-accent-fill", "#5e5cd8")]
    [InlineData("--omni-color-info-fill", "#2196f3")]
    [InlineData("--omni-color-danger-fill", "#f44336")]
    [InlineData("--omni-color-success-fill", "#48a64c")]
    [InlineData("--omni-color-warning-fill", "#d07c00")]
    [InlineData("--omni-color-accent-fill-hover", "#4b4aad")]
    [InlineData("--omni-color-accent-fill-active", "#3e3d8f")]
    [InlineData("--omni-color-neutral-fill", "#e4e3eb")]
    [InlineData("--omni-color-neutral-fill-hover", "#d4d3da")]
    [InlineData("--omni-color-neutral-fill-active", "#c7c6cd")]
    [InlineData("--omni-color-info-bright", "#2196f3")]
    [InlineData("--omni-color-success-bright", "#48a64c")]
    [InlineData("--omni-color-warning-deep", "#a26000")]
    [InlineData("--omni-color-danger-deep", "#d13a2e")]
    public void The_default_palette_matches_the_reference_mockup_in_light_mode(string token, string expected)
    {
        var palette = OmniThemePalettes.All.Single(entry => entry.Name == "Essentiel");

        Assert.Equal(expected, palette.Light[token]);
    }

    [Fact]
    public void The_catalogues_ship_ten_themes_and_ten_palettes_default_first_each_theme_naming_one()
    {
        Assert.Equal(10, OmniThemePresets.All.Count);
        Assert.Equal(10, OmniThemePalettes.All.Count);
        Assert.Equal("Essentiel", OmniThemePresets.All[0].Name);
        Assert.Equal("Essentiel", OmniThemePalettes.All[0].Name);
        Assert.Equal(10, OmniThemePresets.All.Select(preset => preset.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(10, OmniThemePalettes.All.Select(palette => palette.Name).Distinct(StringComparer.Ordinal).Count());

        var defaults = ThemeCatalog.All.Select(theme => theme.DefaultPalette).ToArray();
        Assert.Equal(10, defaults.Distinct(StringComparer.Ordinal).Count());
        Assert.All(defaults, name => Assert.Contains(OmniThemePalettes.All, palette => palette.Name == name));
        Assert.All(OmniThemePresets.All, preset =>
        {
            Assert.NotEmpty(preset.Light);
            Assert.All(preset.Light.Keys, key => Assert.True(preset.Dark.ContainsKey(key), $"{preset.Name}: {key} has no dark value."));
            Assert.All(preset.Dark.Keys, key => Assert.True(
                preset.Light.ContainsKey(key) || preset.DarkShape.ContainsKey(key),
                $"{preset.Name}: {key} has no light value and is not a dark-only shape token."));
            Assert.Same(preset.Dark, preset.For(OmniAppearance.Dark));
            Assert.Same(preset.Light, preset.For(OmniAppearance.System));
        });
    }

    /// <summary>
    /// Ten themes that differed only by a detail would be one theme ten times: every pair differs on
    /// at least three of the tokens that decide how things are drawn (PLAN-008 lot 6).
    /// </summary>
    [Fact]
    public void Every_pair_of_themes_differs_on_at_least_three_shape_tokens()
    {
        string[] shapeTokens =
        [
            "--omni-radius", "--omni-button-radius", "--omni-card-radius", "--omni-border-width",
            "--omni-card-shadow", "--omni-button-shadow", "--omni-font-family", "--omni-button-text-transform",
        ];
        var themes = OmniThemePresets.All;
        for (var first = 0; first < themes.Count; first++)
        {
            for (var second = first + 1; second < themes.Count; second++)
            {
                var differing = shapeTokens.Count(token =>
                    themes[first].Shape.GetValueOrDefault(token, "default") != themes[second].Shape.GetValueOrDefault(token, "default"));
                Assert.True(differing >= 3, $"{themes[first].Name} and {themes[second].Name} differ on {differing} shape tokens only.");
            }
        }
    }

    /// <summary>
    /// A shape follows any palette only if it writes no colour of its own. Neutral shadows are the one
    /// tolerated exception (PLAN-008 lot 6).
    /// </summary>
    [Fact]
    public void No_theme_shape_writes_a_colour_of_its_own()
    {
        foreach (var theme in ThemeCatalog.All)
        {
            foreach (var (name, value) in theme.Shape.Concat(theme.DarkShape))
            {
                var neutral = value.Replace("rgb(0 0 0 /", string.Empty, StringComparison.Ordinal)
                    .Replace("rgb(255 255 255 /", string.Empty, StringComparison.Ordinal)
                    .Replace("0 0 #0000", string.Empty, StringComparison.Ordinal);
                Assert.False(neutral.Contains('#', StringComparison.Ordinal) || neutral.Contains("rgb(", StringComparison.Ordinal),
                    $"{theme.Name}: {name} writes a colour of its own ({value}).");
            }
        }
    }

    /// <summary>
    /// A badge variant carries a meaning, not a colour (RET-002 n°17): in every palette and mode, the
    /// success, warning and danger fills stay apart. The distance is CIE76 in CIELAB (D65). Around 2.3
    /// is the just noticeable difference and up to 10 reads as a difference seen at a glance; 20, twice
    /// that upper bound, is required so that two fills side by side read as two colours, not as two
    /// shades of one.
    /// </summary>
    [Fact]
    public void Every_palette_keeps_its_severities_apart_in_both_modes()
    {
        string[] severities = ["success", "warning", "danger"];
        foreach (var palette in OmniThemePalettes.All)
        {
            foreach (var mode in new[] { OmniAppearance.Light, OmniAppearance.Dark })
            {
                var tokens = palette.For(mode);
                for (var first = 0; first < severities.Length; first++)
                {
                    for (var second = first + 1; second < severities.Length; second++)
                    {
                        var distance = DeltaE(tokens[$"--omni-color-{severities[first]}-fill"], tokens[$"--omni-color-{severities[second]}-fill"]);
                        Assert.True(distance >= 20, $"{palette.Name} in {mode}: {severities[first]} and {severities[second]} fills are {distance:F1} apart in CIELAB.");
                    }
                }
            }
        }
    }

    private static double DeltaE(string first, string second)
    {
        var (l1, a1, b1) = Lab(first);
        var (l2, a2, b2) = Lab(second);
        return Math.Sqrt(Math.Pow(l1 - l2, 2) + Math.Pow(a1 - a2, 2) + Math.Pow(b1 - b2, 2));
    }

    private static (double L, double A, double B) Lab(string hex)
    {
        var (r, g, b) = ThemeColor.Parse(hex);
        var (lr, lg, lb) = (Linear(r), Linear(g), Linear(b));
        var x = Pivot(((lr * 0.4124) + (lg * 0.3576) + (lb * 0.1805)) / 0.95047);
        var y = Pivot((lr * 0.2126) + (lg * 0.7152) + (lb * 0.0722));
        var z = Pivot(((lr * 0.0193) + (lg * 0.1192) + (lb * 0.9505)) / 1.08883);
        return ((116 * y) - 16, 500 * (x - y), 200 * (y - z));

        static double Linear(int channel)
        {
            var value = channel / 255d;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        static double Pivot(double value) => value > 0.008856 ? Math.Cbrt(value) : (7.787 * value) + (16d / 116);
    }

    [Fact]
    public void A_palette_without_a_dark_accent_of_its_own_paints_its_buttons_one_colour_in_both_modes()
    {
        // Aetheus recette R-053: the primary button is the same colour in light and dark by default.
        foreach (var palette in OmniThemePalettes.All.Where(entry => entry.Name != "Mono"))
        {
            Assert.Equal(palette.Light["--omni-color-accent-fill"], palette.Dark["--omni-color-accent-fill"]);
        }
    }
}
