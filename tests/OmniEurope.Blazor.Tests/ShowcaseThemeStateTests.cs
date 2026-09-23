using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;
using System.Net;
using System.Text.Json;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Showcase.Theming;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Exercises the state behind the theme customizer.
/// </summary>
/// <remarks>
/// Everything the customizer page does goes through this type, and every one of its operations ends
/// in a call to the browser. Rendering the page proves the markup builds; only driving the state
/// directly proves that a theme comes with its own palette, that a palette repaints a theme without
/// replacing it, that both halves reach the page, that the export carries the combination on the
/// theme scope, and that a half-written entry in the browser store cannot take the site down.
/// </remarks>
public sealed class ShowcaseThemeStateTests
{
    private const string Css = """
        :root {
            --omni-color-accent: #2563eb;
            --omni-color-surface: #ffffff;
            --omni-radius-medium: 0.5rem;
        }
        """;

    private const string LightSelectors = "[data-omni-theme=\"system\"] {";
    private const string DarkSelector = "[data-omni-theme=\"dark\"] {";
    private const string DarkMedia = "@media (prefers-color-scheme: dark) {";

    [Fact]
    public async Task Initialize_ReadsTheCatalogueAndPushesTheDefaultCombination()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);

        await state.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, state.Tokens.Count);
        Assert.Empty(state.Edits);
        Assert.Same(OmniThemePresets.All[0], state.Theme);
        Assert.Equal("Essentiel", state.Palette.Name);
        Assert.True(state.HasThemePalette);
        var (identifier, arguments) = js.Calls[^1];
        Assert.Equal("omniShowcaseTheme.apply", identifier);
        Assert.Equal(OmniThemePresets.All[0].Light["--omni-color-surface"], Half(arguments[0])["--omni-color-surface"]);
        Assert.Equal(OmniThemePresets.All[0].Dark["--omni-color-surface"], Half(arguments[1])["--omni-color-surface"]);
        Assert.Equal("light", arguments[2]);
    }

    [Fact]
    public async Task Initialize_ReplaysWhatTheBrowserKept()
    {
        var js = new RecordingJsRuntime
        {
            Stored = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["theme"] = "Galet",
                ["palette"] = "Braise",
                ["mode"] = "System",
                ["density"] = "Compact",
                ["--omni-color-accent"] = "#ff0000"
            })
        };
        var state = StateOver(js);

        await state.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Galet", state.Theme.Name);
        Assert.Equal("Braise", state.Palette.Name);
        Assert.Equal(ThemeMode.System, state.Mode);
        Assert.Equal(OmniDensity.Compact, state.Density);
        Assert.Equal("#ff0000", state.Edits["--omni-color-accent"]);
        Assert.Equal("#ff0000", state.Dark["--omni-color-accent"]);
        Assert.Equal("system", js.Calls[^1].Arguments[2]);
    }

    /// <summary>An entry written before themes and palettes existed is a plain map of edited tokens.</summary>
    [Fact]
    public async Task Initialize_ReadsAnEntryWrittenBeforeThemesAndPalettes()
    {
        var js = new RecordingJsRuntime
        {
            Stored = JsonSerializer.Serialize(new Dictionary<string, string> { ["--omni-color-accent"] = "#ff0000" })
        };
        var state = StateOver(js);

        await state.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.Equal("#ff0000", state.Edits["--omni-color-accent"]);
        Assert.Same(OmniThemePresets.All[0], state.Theme);
    }

    [Fact]
    public async Task Initialize_IgnoresNamesTheCatalogueNoLongerHas()
    {
        var js = new RecordingJsRuntime
        {
            Stored = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["theme"] = "Disparu",
                ["palette"] = "Disparue",
                ["mode"] = "Ultraviolet",
                ["density"] = "42"
            })
        };
        var state = StateOver(js);

        await state.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.Same(OmniThemePresets.All[0], state.Theme);
        Assert.True(state.HasThemePalette);
        Assert.Equal(ThemeMode.Light, state.Mode);
        Assert.Equal(OmniDensity.Comfortable, state.Density);
    }

    [Fact]
    public async Task Initialize_SurvivesAHalfWrittenEntry()
    {
        var js = new RecordingJsRuntime { Stored = "{ this is not json" };
        var state = StateOver(js);

        await state.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.Empty(state.Edits);
        Assert.Equal(3, state.Tokens.Count);
    }

    /// <summary>
    /// The public model has no link from a theme to its palette; the state finds it back. It must
    /// be the palette the catalogue declares for every theme, not merely some palette.
    /// </summary>
    [Fact]
    public void DefaultPaletteOf_FindsThePaletteTheCatalogueDeclaresForEveryTheme()
    {
        Assert.Equal(ThemeCatalog.All.Count, OmniThemePresets.All.Count);
        for (var index = 0; index < ThemeCatalog.All.Count; index++)
        {
            Assert.Equal(ThemeCatalog.All[index].DefaultPalette, ThemeState.DefaultPaletteOf(OmniThemePresets.All[index]).Name);
        }
    }

    [Fact]
    public async Task SelectTheme_PaintsItWithItsOwnPalette()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        await state.SelectPaletteAsync(Palette("Mono"), TestContext.Current.CancellationToken);

        foreach (var theme in OmniThemePresets.All)
        {
            await state.SelectThemeAsync(theme, TestContext.Current.CancellationToken);

            Assert.Same(theme, state.Theme);
            Assert.True(state.HasThemePalette, $"{theme.Name} kept the palette picked before it.");
            Assert.Equal(theme.Light, state.Light);
            Assert.Equal(theme.Dark, state.Dark);
        }
    }

    [Fact]
    public async Task SelectPalette_RepaintsTheThemeWithoutReplacingIt()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        var galet = Theme("Galet");
        var ocean = Palette("Océan");
        await state.SelectThemeAsync(galet, TestContext.Current.CancellationToken);

        await state.SelectPaletteAsync(ocean, TestContext.Current.CancellationToken);

        Assert.Same(galet, state.Theme);
        Assert.Same(ocean, state.Palette);
        Assert.False(state.HasThemePalette);
        Assert.Equal(galet.With(ocean).Light, state.Light);
        Assert.Equal(galet.With(ocean).Dark, state.Dark);
    }

    [Fact]
    public async Task ResetPalette_ReturnsToThePaletteOfTheTheme()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        await state.SelectThemeAsync(Theme("Néon"), TestContext.Current.CancellationToken);
        await state.SelectPaletteAsync(Palette("Forêt"), TestContext.Current.CancellationToken);

        await state.ResetPaletteAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Néon", state.Theme.Name);
        Assert.Equal("Électrique", state.Palette.Name);
        Assert.True(state.HasThemePalette);
    }

    [Fact]
    public async Task Set_KeepsOnlyTheTokensMovedAwayFromTheValueInForce()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        var accent = state.Tokens.Single(token => token.Name == "--omni-color-accent");
        var inForce = state.ValueOf(accent);

        await state.SetAsync(accent, "#ff0000", TestContext.Current.CancellationToken);
        Assert.Equal("#ff0000", state.ValueOf(accent));
        Assert.Equal("#ff0000", state.Light["--omni-color-accent"]);
        Assert.Equal("#ff0000", state.Dark["--omni-color-accent"]);

        await state.SetAsync(accent, inForce, TestContext.Current.CancellationToken);
        Assert.Empty(state.Edits);
        Assert.Equal(inForce, state.ValueOf(accent));
    }

    [Fact]
    public async Task ChoosingAnotherThemeOrPalette_DropsTheEdits()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        var accent = state.Tokens.Single(token => token.Name == "--omni-color-accent");

        await state.SetAsync(accent, "#123456", TestContext.Current.CancellationToken);
        await state.SelectPaletteAsync(Palette("Prune"), TestContext.Current.CancellationToken);
        Assert.Empty(state.Edits);

        await state.SetAsync(accent, "#123456", TestContext.Current.CancellationToken);
        await state.SelectThemeAsync(Theme("Octet"), TestContext.Current.CancellationToken);
        Assert.Empty(state.Edits);
    }

    [Fact]
    public async Task SetMode_SendsBothHalvesAndTheModeAndKeepsTheEdits()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        var accent = state.Tokens.Single(token => token.Name == "--omni-color-accent");
        await state.SetAsync(accent, "#abcdef", TestContext.Current.CancellationToken);

        await state.SetModeAsync(ThemeMode.Dark, TestContext.Current.CancellationToken);
        Assert.Equal(ThemeMode.Dark, state.Mode);
        Assert.Equal("dark", js.Calls[^1].Arguments[2]);
        Assert.Equal("#abcdef", state.Edits["--omni-color-accent"]);
        Assert.Equal(state.Dark["--omni-color-surface"], Half(js.Calls[^1].Arguments[1])["--omni-color-surface"]);

        await state.SetModeAsync(ThemeMode.System, TestContext.Current.CancellationToken);
        Assert.Equal("system", js.Calls[^1].Arguments[2]);
    }

    [Fact]
    public async Task SetDensity_IsKeptForTheNextVisit()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);

        await state.SetDensityAsync(OmniDensity.Spacious, TestContext.Current.CancellationToken);

        Assert.Equal(OmniDensity.Spacious, state.Density);
        var stored = JsonSerializer.Deserialize<Dictionary<string, string>>((string)js.Calls[^1].Arguments[4]!)!;
        Assert.Equal("Spacious", stored["density"]);
    }

    [Fact]
    public async Task Reset_DropsTheEditsAndKeepsTheCombination()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        await state.SelectThemeAsync(Theme("Halo"), TestContext.Current.CancellationToken);
        await state.SelectPaletteAsync(Palette("Mono"), TestContext.Current.CancellationToken);
        var accent = state.Tokens.Single(token => token.Name == "--omni-color-accent");
        await state.SetAsync(accent, "#123456", TestContext.Current.CancellationToken);

        await state.ResetAsync(TestContext.Current.CancellationToken);

        Assert.Empty(state.Edits);
        Assert.Equal("Halo", state.Theme.Name);
        Assert.Equal("Mono", state.Palette.Name);
    }

    /// <summary>
    /// PLAN-008 lot 9, Contrôle: a theme exported with a palette that is not its own carries the
    /// palette's colours and the theme's shape, in both halves.
    /// </summary>
    [Fact]
    public async Task Export_OfAThemeWithAForeignPalette_CarriesThePaletteColoursAndTheThemeShape()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        var galet = Theme("Galet");
        var ocean = Palette("Océan");
        Assert.NotEqual("Océan", ThemeState.DefaultPaletteOf(galet).Name);
        await state.SelectThemeAsync(galet, TestContext.Current.CancellationToken);
        await state.SelectPaletteAsync(ocean, TestContext.Current.CancellationToken);

        var css = state.ExportCss();
        var light = Block(css, LightSelectors);
        var dark = Block(css, DarkSelector);

        Assert.Contains("theme Galet, palette Océan", css, StringComparison.Ordinal);
        var shaped = galet.Shape.Keys.Concat(galet.DarkShape.Keys).ToHashSet(StringComparer.Ordinal);
        foreach (var (name, value) in ocean.Light.Where(entry => !shaped.Contains(entry.Key)))
        {
            Assert.Contains($"{name}: {value};", light, StringComparison.Ordinal);
        }

        foreach (var (name, value) in ocean.Dark.Where(entry => !shaped.Contains(entry.Key)))
        {
            Assert.Contains($"{name}: {value};", dark, StringComparison.Ordinal);
        }

        Assert.NotEmpty(galet.Shape);
        foreach (var (name, value) in galet.Shape)
        {
            Assert.Contains($"{name}: {value};", light, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// The dark shape of a theme (Galet's cards drawn differently in dark) goes to both dark
    /// variants, the dark scope and the system scope under a dark setting, and not to the light half.
    /// </summary>
    [Fact]
    public async Task Export_CarriesTheDarkShapeInBothDarkVariantsOnly()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        var galet = Theme("Galet");
        Assert.NotEmpty(galet.DarkShape);
        await state.SelectThemeAsync(galet, TestContext.Current.CancellationToken);
        await state.SelectPaletteAsync(Palette("Lavande"), TestContext.Current.CancellationToken);

        var css = state.ExportCss();
        var light = Block(css, LightSelectors);
        var dark = Block(css, DarkSelector);
        var media = Block(css[css.IndexOf(DarkMedia, StringComparison.Ordinal)..], LightSelectors);

        foreach (var (name, value) in galet.DarkShape)
        {
            var declaration = $"{name}: {value};";
            Assert.Contains(declaration, dark, StringComparison.Ordinal);
            Assert.Contains(declaration, media, StringComparison.Ordinal);
            if (!galet.Shape.TryGetValue(name, out var lightValue) || lightValue != value)
            {
                Assert.DoesNotContain(declaration, light, StringComparison.Ordinal);
            }
        }
    }

    /// <summary>
    /// The stylesheet redeclares its tokens on every theme scope: a value written on :root alone is
    /// shadowed inside an OmniThemeScope. The export names the scope in every mode.
    /// </summary>
    [Fact]
    public async Task Export_TargetsTheThemeScopeInLightDarkAndSystem()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);

        var css = state.ExportCss();

        Assert.Contains(":root,\n[data-omni-theme=\"light\"],\n[data-omni-theme=\"system\"] {", css.ReplaceLineEndings("\n"), StringComparison.Ordinal);
        Assert.Contains(DarkSelector, css, StringComparison.Ordinal);
        Assert.Contains(DarkMedia, css, StringComparison.Ordinal);
        Assert.Equal(2, css.Split(LightSelectors).Length - 1);
        Assert.True(
            css.IndexOf(DarkSelector, StringComparison.Ordinal) > css.IndexOf(LightSelectors, StringComparison.Ordinal)
            && css.IndexOf(DarkMedia, StringComparison.Ordinal) > css.IndexOf(DarkSelector, StringComparison.Ordinal),
            "The dark variants come after the light one, so they win at equal specificity.");
    }

    [Fact]
    public async Task Export_WritesTheEditsInBothHalves()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        var accent = state.Tokens.Single(token => token.Name == "--omni-color-accent");
        await state.SetAsync(accent, "#ff0000", TestContext.Current.CancellationToken);

        var css = state.ExportCss();

        Assert.Equal(3, css.Split("--omni-color-accent: #ff0000;").Length - 1);
        Assert.Contains("1 token(s) edited", css, StringComparison.Ordinal);
    }

    /// <summary>
    /// A theme sets tokens the stylesheet resolves at the point of use and never declares at the
    /// root (the button press, the card border width): the export carries them after the catalogue
    /// tokens, which keep stylesheet order.
    /// </summary>
    [Fact]
    public async Task Export_WritesTheCatalogueTokensFirstThenThoseBeyondIt()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        await state.SelectThemeAsync(Theme("Galet"), TestContext.Current.CancellationToken);

        var light = Block(state.ExportCss(), LightSelectors);

        Assert.Contains("--omni-button-radius:", light, StringComparison.Ordinal);
        Assert.True(
            light.IndexOf("--omni-color-accent:", StringComparison.Ordinal) < light.IndexOf("--omni-color-surface:", StringComparison.Ordinal)
            && light.IndexOf("--omni-color-surface:", StringComparison.Ordinal) < light.IndexOf("--omni-button-radius:", StringComparison.Ordinal),
            "The catalogue tokens come first, in stylesheet order.");
    }

    [Fact]
    public async Task EveryChange_IsAnnouncedToOpenEditors()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);
        var announced = 0;
        state.Changed += () => announced++;

        await state.InitializeAsync(TestContext.Current.CancellationToken);
        await state.SelectThemeAsync(Theme("Rétro"), TestContext.Current.CancellationToken);
        await state.SelectPaletteAsync(Palette("Mono"), TestContext.Current.CancellationToken);
        await state.SetModeAsync(ThemeMode.Dark, TestContext.Current.CancellationToken);
        await state.SetDensityAsync(OmniDensity.Compact, TestContext.Current.CancellationToken);
        await state.ResetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(6, announced);
    }

    private static OmniThemePreset Theme(string name) => OmniThemePresets.All.Single(theme => theme.Name == name);

    private static OmniThemePalette Palette(string name) => OmniThemePalettes.All.Single(palette => palette.Name == name);

    private static IReadOnlyDictionary<string, string> Half(object? argument) =>
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(argument);

    /// <summary>The declarations of the first block opened by <paramref name="header"/>.</summary>
    private static string Block(string css, string header)
    {
        var start = css.IndexOf(header, StringComparison.Ordinal);
        Assert.True(start >= 0, $"No block opens with {header}.");
        var end = css.IndexOf('}', start);
        return css[start..end];
    }

    private static ThemeState StateOver(RecordingJsRuntime js)
    {
        var http = new HttpClient(new StubHandler(Css)) { BaseAddress = new Uri("https://localhost/") };
        return new ThemeState(new ThemeTokenReader(http), js);
    }

    private sealed class StubHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
    }

    private sealed class RecordingJsRuntime : IJSRuntime
    {
        public string? Stored { get; set; }

        public List<(string Identifier, object?[] Arguments)> Calls { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Calls.Add((identifier, args ?? []));
            return identifier is "omniShowcaseTheme.load" && Stored is TValue stored
                ? ValueTask.FromResult(stored)
                : ValueTask.FromResult<TValue>(default!);
        }
    }
}
