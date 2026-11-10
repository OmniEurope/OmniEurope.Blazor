using System.Globalization;
using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// recette R1-17: a navigation tile of a tile grid given no tone takes a hue of its own, the hue of its rank
/// among the tiles of the grid, six hues in turn; a tone set explicitly wins, and a tile outside a grid keeps the
/// accent. The classes and the rules are proved here, the hues on every theme and palette in both modes; the colours
/// measured in a real browser (see the showcase).
/// </summary>
public sealed class NavTileAutoHueTests : OmniBunitContext
{
    private const double Text = 4.5;

    // The ANSI token each rank falls back to, in the order of the rules: amber, blue, red, cyan, magenta, green.
    private static readonly int[] AnsiOfRank = [11, 12, 9, 14, 13, 10];

    [Fact]
    public void Tiles_of_a_grid_given_no_tone_take_the_automatic_hue()
    {
        var grid = Render<OmniTileGrid>(parameters =>
        {
            for (var index = 0; index < 7; index++)
            {
                var title = $"T{index}";
                parameters.AddChildContent<OmniNavTile>(tile => tile.Add(component => component.Href, "/a").Add(component => component.Title, title));
            }
        });

        var tiles = grid.FindAll("a.omni-nav-tile");
        Assert.Equal(7, tiles.Count);
        Assert.All(tiles, tile =>
        {
            Assert.Contains("omni-nav-tile--auto", tile.ClassList);
            Assert.DoesNotContain("omni-nav-tile--accent", tile.ClassList);
        });
    }

    /// <summary>A favourite tile sits in its host box, a direct child of the grid that the rank rules count, and keeps the automatic hue class.</summary>
    [Fact]
    public void A_favourite_tile_of_a_grid_takes_the_automatic_hue_in_its_host_box()
    {
        var grid = Render<OmniTileGrid>(parameters => parameters
            .AddChildContent<OmniNavTile>(tile => tile.Add(component => component.Href, "/a").Add(component => component.Title, "A"))
            .AddChildContent<OmniNavTile>(tile => tile.Add(component => component.Href, "/b").Add(component => component.Title, "B").Add(component => component.IsFavorite, true)));

        var favourite = grid.Find(".omni-tile-grid > .omni-nav-tile-host > a.omni-nav-tile");
        Assert.Contains("omni-nav-tile--auto", favourite.ClassList);
        Assert.Contains("omni-nav-tile--auto", grid.Find(".omni-tile-grid > a.omni-nav-tile").ClassList);
    }

    [Theory]
    [InlineData(OmniTone.Accent, "omni-nav-tile--accent")]
    [InlineData(OmniTone.Success, "omni-nav-tile--success")]
    [InlineData(OmniTone.Neutral, "omni-nav-tile--neutral")]
    public void A_tone_set_explicitly_in_a_grid_wins_over_the_automatic_hue(OmniTone tone, string expected)
    {
        var grid = Render<OmniTileGrid>(parameters => parameters
            .AddChildContent<OmniNavTile>(tile => tile.Add(component => component.Href, "/a").Add(component => component.Title, "A"))
            .AddChildContent<OmniNavTile>(tile => tile.Add(component => component.Href, "/b").Add(component => component.Title, "B").Add(component => component.Tone, tone)));

        var tiles = grid.FindAll("a.omni-nav-tile");
        Assert.Contains("omni-nav-tile--auto", tiles[0].ClassList);
        Assert.Contains(expected, tiles[1].ClassList);
        Assert.DoesNotContain("omni-nav-tile--auto", tiles[1].ClassList);
    }

    [Fact]
    public void A_tile_outside_a_grid_keeps_the_accent()
    {
        var tile = Render<OmniNavTile>(parameters => parameters.Add(component => component.Href, "/a").Add(component => component.Title, "A"));

        Assert.Contains("omni-nav-tile--accent", tile.Find("a").ClassList);
        Assert.DoesNotContain("omni-nav-tile--auto", tile.Find("a").ClassList);
    }

    [Fact]
    public void A_tone_given_on_a_later_render_replaces_the_automatic_hue()
    {
        var grid = Render<OmniTileGrid>(parameters => parameters
            .AddChildContent<OmniNavTile>(tile => tile.Add(component => component.Href, "/a").Add(component => component.Title, "A")));
        var tile = grid.FindComponent<OmniNavTile>();

        tile.Render(parameters => parameters.Add(component => component.Tone, OmniTone.Warning));

        Assert.Contains("omni-nav-tile--warning", grid.Find("a").ClassList);
        Assert.DoesNotContain("omni-nav-tile--auto", grid.Find("a").ClassList);
    }

    /// <summary>The six ranks draw their hue from the host's token, else the theme's bright ANSI colour, and the square is tinted at the share the ANSI colours are held for.</summary>
    [Fact]
    public void The_stylesheet_gives_each_rank_its_hue_and_tints_the_square_at_the_held_share()
    {
        string[] ranks = ["6n + 1", "6n + 2", "6n + 3", "6n + 4", "6n + 5", "6n"];
        for (var rank = 0; rank < ranks.Length; rank++)
        {
            // The rank counts the tiles and the host boxes of favourite tiles together; the rule reaches the tile in either.
            var rankOf = $":nth-child({ranks[rank]} of .omni-nav-tile, .omni-nav-tile-host)";
            var body = ShippedLookTests.Body($".omni-tile-grid > .omni-nav-tile--auto{rankOf}, .omni-tile-grid > .omni-nav-tile-host{rankOf} > .omni-nav-tile--auto");
            Assert.Equal($"var(--omni-tile-hue-{rank + 1}, var(--omni-ansi-{AnsiOfRank[rank]}))", ShippedLookTests.Value(body, "--omni-nav-tile-tone"));
        }

        var share = string.Create(CultureInfo.InvariantCulture, $"{AnsiTokenBuilder.BackgroundShare * 100:0}%");
        Assert.Equal(
            $"color-mix(in srgb, var(--omni-nav-tile-tone) {share}, var(--omni-card-background, var(--omni-color-surface)))",
            ShippedLookTests.Value(ShippedLookTests.Body(".omni-tile-grid > .omni-nav-tile--auto, .omni-tile-grid > .omni-nav-tile-host > .omni-nav-tile--auto"), "--omni-nav-tile-tint"));
    }

    /// <summary>On every theme and palette, in both modes, the icon of each hue reads at 4.5:1 on its tinted square and on the tile.</summary>
    [Theory]
    [MemberData(nameof(ThemeContrastMatrixTests.Sets), MemberType = typeof(ThemeContrastMatrixTests))]
    public void Each_hue_reads_on_its_square_and_on_the_tile(string themeName, string paletteName, OmniAppearance mode)
    {
        var tokens = TokensOf(themeName, paletteName, mode);
        var tile = ShowcaseThemeTests.Resolve(tokens, tokens.TryGetValue("--omni-card-background", out var card) ? card : tokens["--omni-color-surface"]);

        var failures = new List<string>();
        foreach (var (rank, hue) in Hues(tokens))
        {
            var square = ThemeColor.Mix(hue, tile, AnsiTokenBuilder.BackgroundShare);
            foreach (var (where, ground) in new[] { ("its square", square), ("the tile", tile) })
            {
                var contrast = ThemeColor.Contrast(hue, ground);
                if (contrast < Text)
                {
                    failures.Add(string.Create(CultureInfo.InvariantCulture, $"hue {rank} ({hue}) on {where} ({ground}): {contrast:F2}, below {Text:F1}"));
                }
            }
        }

        ThemeContrastMatrixTests.AssertOrAccept(themeName, paletteName, mode, failures);
    }

    /// <summary>On every theme and palette, in both modes, the six hues are distinct and each is at least 60 degrees of hue from the next one, the sixth from the first included.</summary>
    [Theory]
    [MemberData(nameof(ThemeContrastMatrixTests.Sets), MemberType = typeof(ThemeContrastMatrixTests))]
    public void Neighbouring_hues_are_far_apart(string themeName, string paletteName, OmniAppearance mode)
    {
        var hues = Hues(TokensOf(themeName, paletteName, mode)).Select(entry => entry.Hue).ToArray();

        Assert.Equal(6, hues.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        for (var rank = 0; rank < hues.Length; rank++)
        {
            var next = hues[(rank + 1) % hues.Length];
            var distance = Math.Abs(HueAngle(hues[rank]) - HueAngle(next));
            distance = Math.Min(distance, 360 - distance);
            Assert.True(distance >= 60, $"{themeName} + {paletteName} in {mode}: hue {rank + 1} ({hues[rank]}) and the next ({next}) are {distance:F0} degrees apart.");
        }
    }

    private static IReadOnlyDictionary<string, string> TokensOf(string themeName, string paletteName, OmniAppearance mode) =>
        OmniThemePresets.All.Single(entry => entry.Name == themeName)
            .With(OmniThemePalettes.All.Single(entry => entry.Name == paletteName))
            .For(mode);

    private static IEnumerable<(int Rank, string Hue)> Hues(IReadOnlyDictionary<string, string> tokens) =>
        AnsiOfRank.Select((ansi, rank) => (rank + 1, ShowcaseThemeTests.Resolve(tokens, tokens[$"--omni-ansi-{ansi}"])));

    private static double HueAngle(string hex)
    {
        var (r, g, b) = ThemeColor.Parse(hex);
        double red = r / 255.0, green = g / 255.0, blue = b / 255.0;
        var max = Math.Max(red, Math.Max(green, blue));
        var delta = max - Math.Min(red, Math.Min(green, blue));
        if (delta == 0)
        {
            return 0;
        }

        var angle = max == red ? 60 * (((green - blue) / delta) % 6)
            : max == green ? 60 * (((blue - red) / delta) + 2)
            : 60 * (((red - green) / delta) + 4);
        return angle < 0 ? angle + 360 : angle;
    }
}
