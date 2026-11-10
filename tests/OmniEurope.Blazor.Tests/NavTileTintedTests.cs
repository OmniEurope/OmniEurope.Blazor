using System.Globalization;
using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniNavTile.Tinted"/> (request of a client application, its home menu): the whole tile takes its tone, the
/// hue of its rank in a tile grid, favourite tiles included. The class and the rules are proved here, and the text kept
/// at 4.5:1 on the strongest tint of every hue on every theme and palette in both modes; the colours measured in a real
/// browser (see the showcase, Navigation tiles page).
/// </summary>
public sealed class NavTileTintedTests : OmniBunitContext
{
    private const double Text = 4.5;

    // The ANSI token of each rank, in the order of the rank rules (see NavTileAutoHueTests).
    private static readonly int[] AnsiOfRank = [11, 12, 9, 14, 13, 10];

    [Fact]
    public void Tinted_tiles_of_a_grid_carry_the_tinted_class_beside_the_automatic_hue_favourites_included()
    {
        var grid = Render<OmniTileGrid>(parameters => parameters
            .AddChildContent<OmniNavTile>(tile => tile.Add(component => component.Href, "/a").Add(component => component.Title, "A").Add(component => component.Tinted, true))
            .AddChildContent<OmniNavTile>(tile => tile.Add(component => component.Href, "/b").Add(component => component.Title, "B").Add(component => component.Tinted, true).Add(component => component.IsFavorite, false)));

        var tiles = grid.FindAll("a.omni-nav-tile");
        Assert.Equal(2, tiles.Count);
        Assert.All(tiles, tile =>
        {
            Assert.Contains("omni-nav-tile--tinted", tile.ClassList);
            Assert.Contains("omni-nav-tile--auto", tile.ClassList);
        });
        Assert.NotNull(grid.Find(".omni-tile-grid > .omni-nav-tile-host > a.omni-nav-tile--tinted"));
    }

    [Fact]
    public void A_tile_is_not_tinted_by_default()
    {
        var tile = Render<OmniNavTile>(parameters => parameters.Add(component => component.Href, "/a").Add(component => component.Title, "A"));

        Assert.DoesNotContain("omni-nav-tile--tinted", tile.Find("a").ClassList);
    }

    /// <summary>The rest and hover gradients mix the tone over the card background at the shares of the mode, light by default.</summary>
    [Fact]
    public void The_stylesheet_tints_the_whole_tile_at_rest_and_stronger_hovered_or_focused()
    {
        string Gradient(string from, string fromDefault, string to, string toDefault) =>
            $"linear-gradient(135deg, color-mix(in srgb, var(--omni-nav-tile-tone) var({from}, {fromDefault}), var(--omni-card-background, var(--omni-color-surface))), color-mix(in srgb, var(--omni-nav-tile-tone) var({to}, {toDefault}), var(--omni-card-background, var(--omni-color-surface))))";

        Assert.Equal(
            Gradient("--omni-nav-tile-tinted-from", "16%", "--omni-nav-tile-tinted-to", "8%"),
            ShippedLookTests.Value(ShippedLookTests.Body(".omni-nav-tile--tinted"), "background"));
        Assert.Equal(
            Gradient("--omni-nav-tile-tinted-hover-from", "34%", "--omni-nav-tile-tinted-hover-to", "20%"),
            ShippedLookTests.Value(ShippedLookTests.Body(".omni-nav-tile--tinted:is(:hover, :focus-visible):not(.omni-nav-tile--disabled)"), "background"));

        Assert.Equal("var(--omni-color-text)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-nav-tile--tinted .omni-nav-tile__description"), "color"));

        // The dark shares, the ones the contrast test below holds to, on the dark theme and on the system theme in a dark scheme.
        var dark = ShippedLookTests.Rules()
            .Where(rule => rule.Selector == "[data-omni-theme=\"dark\"]" || rule.Selector == "[data-omni-theme=\"system\"]")
            .Select(rule => rule.Body)
            .Where(body => body.Contains("--omni-nav-tile-tinted-hover-from", StringComparison.Ordinal))
            .ToList();
        Assert.Equal(2, dark.Count);
        Assert.All(dark, body =>
        {
            Assert.Equal("20%", ShippedLookTests.Value(body, "--omni-nav-tile-tinted-hover-from"));
            Assert.Equal("12%", ShippedLookTests.Value(body, "--omni-nav-tile-tinted-from"));
        });
    }

    /// <summary>
    /// On every theme and palette, in both modes, the text colour reads at 4.5:1 on the strongest tint of each hue. The title and,
    /// on a tinted tile, the description use it: the muted grey of the description would fall to about 3:1 on a 34 % tint.
    /// </summary>
    [Theory]
    [MemberData(nameof(ThemeContrastMatrixTests.Sets), MemberType = typeof(ThemeContrastMatrixTests))]
    public void The_text_reads_on_the_strongest_tint_of_every_hue(string themeName, string paletteName, OmniAppearance mode)
    {
        var tokens = OmniThemePresets.All.Single(entry => entry.Name == themeName)
            .With(OmniThemePalettes.All.Single(entry => entry.Name == paletteName))
            .For(mode);
        var card = ShowcaseThemeTests.Resolve(tokens, tokens.TryGetValue("--omni-card-background", out var background) ? background : tokens["--omni-color-surface"]);
        var share = mode == OmniAppearance.Dark ? 0.20 : 0.34;

        var failures = new List<string>();
        foreach (var (rank, ansi) in AnsiOfRank.Select((ansi, index) => (index + 1, ansi)))
        {
            var hue = ShowcaseThemeTests.Resolve(tokens, tokens[$"--omni-ansi-{ansi}"]);
            var ground = ThemeColor.Mix(hue, card, share);
            foreach (var token in new[] { "--omni-color-text" })
            {
                var text = ShowcaseThemeTests.Resolve(tokens, tokens[token]);
                var contrast = ThemeColor.Contrast(text, ground);
                if (contrast < Text)
                {
                    failures.Add(string.Create(CultureInfo.InvariantCulture, $"{token} ({text}) on hue {rank} tinted at {share:P0} ({ground}): {contrast:F2}, below {Text:F1}"));
                }
            }
        }

        ThemeContrastMatrixTests.AssertOrAccept(themeName, paletteName, mode, failures);
    }
}
