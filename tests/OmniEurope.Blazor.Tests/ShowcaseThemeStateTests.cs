using OmniEurope.Blazor.Components;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Showcase.Theming;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Exercises the state behind the theme customizer, over the package's own <see cref="OmniAppearanceState"/>.
/// </summary>
/// <remarks>
/// Everything the customizer page does goes through this type, and every one of its operations ends
/// in a call to the browser. Rendering the page proves the markup builds; only driving the state
/// directly proves that a theme comes with its own palette, that a palette repaints a theme without
/// replacing it, that both halves reach the page, that the export carries the combination on the
/// theme scope, that every appearance setting lives in the package state (kept as the visitor's preference,
/// replayed by the next visit and followed when another tab changes it), that a look kept under the former
/// showcase entry is moved into the package state once, and that a half-written entry cannot take the site down.
/// The package state reads and writes its preference through <see cref="PreferenceBrowser"/>, the browser cache
/// of <c>omni-preferences.js</c> played over a dictionary.
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
        var (state, _) = StateOver(js);

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
        // The package state's default mode: the system's.
        Assert.Equal("system", arguments[2]);
        Assert.Equal("{}", arguments[4]);
    }

    [Fact]
    public async Task ADarkOnlyTheme_IsDrawnDark_AndKeepsTheVisitorsMode()
    {
        var js = new RecordingJsRuntime();
        var (state, appearance) = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        await state.SetModeAsync(OmniAppearance.Light, TestContext.Current.CancellationToken);

        await state.SelectThemeAsync(OmniThemePresets.All[0] with { DarkOnly = true }, TestContext.Current.CancellationToken);

        // Drawn dark although light is asked; the visitor's choice is kept for the next theme.
        Assert.Equal(OmniAppearance.Dark, state.EffectiveMode);
        Assert.Equal(OmniAppearance.Light, state.Mode);
        Assert.Equal(OmniAppearance.Light, appearance.Appearance);
        Assert.Equal("dark", js.Calls[^1].Arguments[2]);

        await state.SelectThemeAsync(OmniThemePresets.All[0], TestContext.Current.CancellationToken);
        Assert.Equal(OmniAppearance.Light, state.EffectiveMode);
        Assert.Null(appearance.Preset);
    }

    /// <summary>
    /// Every appearance setting is the package state's: set through the showcase, it is written as the package
    /// preference, and a new page over the same browser (a reload) paints it back without the showcase entry.
    /// </summary>
    [Fact]
    public async Task EverySetting_IsThePackageStates_AndComesBackOnReload()
    {
        var js = new RecordingJsRuntime();
        var browser = new PreferenceBrowser();
        var (state, appearance) = StateOver(js, browser);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        var galet = Theme("Galet");
        var font = OmniThemeFonts.All[^1];
        var backdrop = OmniThemeBackdrops.All.First(item => item.Name == "Bokeh");

        await state.SelectThemeAsync(galet, TestContext.Current.CancellationToken);
        await state.SelectPaletteAsync(Palette("Braise"), TestContext.Current.CancellationToken);
        await state.SelectBackdropAsync(backdrop, TestContext.Current.CancellationToken);
        await state.SetModeAsync(OmniAppearance.Dark, TestContext.Current.CancellationToken);
        await state.SetDensityAsync(OmniDensity.Compact, TestContext.Current.CancellationToken);
        await state.SetBackdropMotionAsync(false, TestContext.Current.CancellationToken);
        await state.SetFontAsync(font, TestContext.Current.CancellationToken);
        await state.SetTextSizeLevelAsync(8, TestContext.Current.CancellationToken);
        await state.SetControlSizeLevelAsync(2, TestContext.Current.CancellationToken);
        await state.SetFullWidthAsync(true, TestContext.Current.CancellationToken);

        Assert.Same(galet, appearance.Preset);
        Assert.Equal("Braise", appearance.Palette?.Name);
        Assert.Same(backdrop, appearance.Backdrop);
        Assert.Equal(OmniAppearance.Dark, appearance.Appearance);
        Assert.Equal(OmniDensity.Compact, appearance.Density);
        Assert.False(appearance.BackdropMotion);
        Assert.Same(font, appearance.Font);
        Assert.Equal(8, appearance.TextSizeLevel);
        Assert.Equal(2, appearance.ControlSizeLevel);
        Assert.True(appearance.FullWidth);
        Assert.Contains(browser.Storage.Values, entry => entry.Contains("Galet", StringComparison.Ordinal));
        // The showcase entry keeps the hand edits alone.
        Assert.Equal("{}", js.Calls[^1].Arguments[4]);
        Assert.Equal(8, js.Calls[^1].Arguments[5]);
        Assert.Equal(2, js.Calls[^1].Arguments[6]);

        var reloadJs = new RecordingJsRuntime();
        var (reloaded, _) = StateOver(reloadJs, browser);
        await reloaded.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.Same(galet, reloaded.Theme);
        Assert.Equal("Braise", reloaded.Palette.Name);
        Assert.Same(backdrop, reloaded.DrawnBackdrop);
        Assert.Equal(OmniAppearance.Dark, reloaded.Mode);
        Assert.Equal(OmniDensity.Compact, reloaded.Density);
        Assert.False(reloaded.BackdropMotion);
        Assert.Same(font, reloaded.Font);
        Assert.Equal(8, reloaded.TextSizeLevel);
        Assert.Equal(2, reloaded.ControlSizeLevel);
        Assert.True(reloaded.FullWidth);
        Assert.Equal("dark", reloadJs.Calls[^1].Arguments[2]);
    }

    /// <summary>
    /// A look kept under the former showcase entry, written before the showcase delegated its look to the package,
    /// is moved into the package state; the entry is written back with the edits alone, so the move happens once.
    /// </summary>
    [Fact]
    public async Task Initialize_MovesALookKeptUnderTheFormerEntryIntoThePackageState_Once()
    {
        var js = new RecordingJsRuntime
        {
            Stored = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["theme"] = "Galet",
                ["palette"] = "Braise",
                ["mode"] = "System",
                ["density"] = "Compact",
                ["motion"] = "False",
                ["backdrop"] = "Bokeh",
                ["font"] = OmniThemeFonts.All[^1].Name,
                ["textSize"] = "8",
                ["controlSize"] = "2",
                ["fullWidth"] = "True",
                ["--omni-color-accent"] = "#ff0000"
            })
        };
        var browser = new PreferenceBrowser();
        var (state, appearance) = StateOver(js, browser);

        await state.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Galet", state.Theme.Name);
        Assert.Equal("Braise", state.Palette.Name);
        Assert.Equal(OmniAppearance.System, state.Mode);
        Assert.Equal(OmniDensity.Compact, state.Density);
        Assert.False(state.BackdropMotion);
        Assert.Equal("Bokeh", state.DrawnBackdrop.Name);
        Assert.Same(OmniThemeFonts.All[^1], state.Font);
        Assert.Equal(8, state.TextSizeLevel);
        Assert.Equal(2, state.ControlSizeLevel);
        Assert.True(state.FullWidth);
        Assert.Equal("Galet", appearance.Preset?.Name);
        Assert.Equal("#ff0000", state.Edits["--omni-color-accent"]);
        Assert.Equal("#ff0000", state.Dark["--omni-color-accent"]);
        Assert.Equal("system", js.Calls[^1].Arguments[2]);
        var rewritten = JsonSerializer.Deserialize<Dictionary<string, string>>((string)js.Calls[^1].Arguments[4]!)!;
        Assert.Equal(["--omni-color-accent"], rewritten.Keys);

        // The next visit reads the edits alone from the entry and the look from the package preference.
        var nextJs = new RecordingJsRuntime { Stored = (string)js.Calls[^1].Arguments[4]! };
        var (next, _) = StateOver(nextJs, browser);
        await next.InitializeAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Galet", next.Theme.Name);
        Assert.Equal("Braise", next.Palette.Name);
        Assert.Equal("#ff0000", next.Edits["--omni-color-accent"]);
    }

    /// <summary>An entry written before themes and palettes existed is a plain map of edited tokens.</summary>
    [Fact]
    public async Task Initialize_ReadsAnEntryWrittenBeforeThemesAndPalettes()
    {
        var js = new RecordingJsRuntime
        {
            Stored = JsonSerializer.Serialize(new Dictionary<string, string> { ["--omni-color-accent"] = "#ff0000" })
        };
        var (state, _) = StateOver(js);

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
                ["density"] = "42",
                ["motion"] = "maybe",
                ["backdrop"] = "",
                ["font"] = "Nope",
                ["textSize"] = "abc",
                ["controlSize"] = "11",
                ["fullWidth"] = "maybe"
            })
        };
        var (state, _) = StateOver(js);

        await state.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.Same(OmniThemePresets.All[0], state.Theme);
        Assert.True(state.HasThemePalette);
        Assert.Equal(OmniAppearance.System, state.Mode);
        Assert.Equal(OmniDensity.Comfortable, state.Density);
        Assert.True(state.BackdropMotion);
        Assert.Same(OmniThemeBackdrops.None, state.DrawnBackdrop);
        Assert.Null(state.Font);
        Assert.Equal(AppearanceLookDefaultLevel, state.TextSizeLevel);
        Assert.Equal(AppearanceLookDefaultLevel, state.ControlSizeLevel);
        Assert.False(state.FullWidth);
    }

    /// <summary>A combination kept under a name the catalogue has since renamed comes back under the new name.</summary>
    [Theory]
    [InlineData("Défaut", "Crépuscule", "Essentiel", "Opale")]
    [InlineData("Galet", "Défaut", "Galet", "Essentiel")]
    public async Task Initialize_MapsRenamedThemeAndPaletteNames(string storedTheme, string storedPalette, string theme, string palette)
    {
        var js = new RecordingJsRuntime
        {
            Stored = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["theme"] = storedTheme,
                ["palette"] = storedPalette
            })
        };
        var (state, _) = StateOver(js);

        await state.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.Equal(theme, state.Theme.Name);
        Assert.Equal(palette, state.Palette.Name);
    }

    [Theory]
    [InlineData("{ this is not json")]
    [InlineData("null")]
    public async Task Initialize_SurvivesAHalfWrittenEntry(string stored)
    {
        var js = new RecordingJsRuntime { Stored = stored };
        var (state, _) = StateOver(js);

        await state.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.Empty(state.Edits);
        Assert.Equal(3, state.Tokens.Count);
    }

    /// <summary>
    /// Another tab changes the look: the package state receives it through the browser's storage event, and the
    /// showcase repaints the page with it, dropping the edits made for the previous theme.
    /// </summary>
    [Fact]
    public async Task AChangeMadeInAnotherTab_RepaintsThePage()
    {
        var firstJs = new RecordingJsRuntime();
        var firstBrowser = new PreferenceBrowser();
        var (first, _) = StateOver(firstJs, firstBrowser);
        await first.InitializeAsync(TestContext.Current.CancellationToken);
        var secondJs = new RecordingJsRuntime();
        var secondBrowser = new PreferenceBrowser();
        var (second, _) = StateOver(secondJs, secondBrowser);
        await second.InitializeAsync(TestContext.Current.CancellationToken);
        var accent = second.Tokens.Single(token => token.Name == "--omni-color-accent");
        await second.SetAsync(accent, "#123456", TestContext.Current.CancellationToken);
        var announced = 0;
        second.Changed += () => announced++;

        await first.SelectThemeAsync(Theme("Givre"), TestContext.Current.CancellationToken);
        foreach (var (key, value) in firstBrowser.Storage)
        {
            secondBrowser.Storage[key] = value;
            await secondBrowser.Bridge!.OnStored(key);
        }

        Assert.Equal("Givre", second.Theme.Name);
        Assert.Empty(second.Edits);
        Assert.True(announced > 0);
        Assert.Equal(Theme("Givre").Light["--omni-color-accent"], Half(secondJs.Calls[^1].Arguments[0])["--omni-color-accent"]);
    }

    /// <summary>A change of the package state from elsewhere drops the edits of a backdrop no longer drawn, and keeps the others.</summary>
    [Fact]
    public async Task ABackdropChangedElsewhere_DropsTheEditsOfTheBackdropItLeaves()
    {
        var js = new RecordingJsRuntime();
        var (state, appearance) = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        await state.SelectThemeAsync(Theme("Givre"), TestContext.Current.CancellationToken);
        var leaving = state.DrawnBackdrop;
        var backdropToken = leaving.Shape.Keys.First();
        var accent = state.Tokens.Single(token => token.Name == "--omni-color-accent");
        await state.SetAsync(new ThemeToken(backdropToken, "shipped", ThemeTokenGroup.Color), "edited-by-hand", TestContext.Current.CancellationToken);
        await state.SetAsync(accent, "#123456", TestContext.Current.CancellationToken);

        await appearance.SetBackdropAsync(OmniThemeBackdrops.None);

        Assert.Same(OmniThemeBackdrops.None, state.DrawnBackdrop);
        Assert.False(state.Edits.ContainsKey(backdropToken));
        Assert.Equal("#123456", state.Edits["--omni-color-accent"]);

        // A setting that is neither the theme, the palette nor the backdrop keeps every edit.
        await appearance.SetDensityAsync(OmniDensity.Spacious);
        Assert.Equal("#123456", state.Edits["--omni-color-accent"]);
    }

    /// <summary>Before the showcase paints, a change of the package state is only noted: nothing reaches the browser.</summary>
    [Fact]
    public async Task AChangeBeforeInitialization_IsNotPushed()
    {
        var js = new RecordingJsRuntime();
        var (state, appearance) = StateOver(js);
        var announced = 0;
        state.Changed += () => announced++;

        await appearance.SetPresetAsync(Theme("Halo"));
        await state.SetModeAsync(OmniAppearance.Dark, TestContext.Current.CancellationToken);

        Assert.Equal("Halo", state.Theme.Name);
        Assert.Equal(2, announced);
        Assert.DoesNotContain(js.Calls, call => call.Identifier == "omniShowcaseTheme.apply");
        Assert.Equal("Halo", state.Painted.Name);

        state.Dispose();
        await appearance.SetPresetAsync(Theme("Galet"));
        Assert.Equal(2, announced);
    }

    [Fact]
    public async Task SelectTheme_PaintsItWithItsOwnPalette()
    {
        var js = new RecordingJsRuntime();
        var (state, _) = StateOver(js);
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
        var (state, appearance) = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        var galet = Theme("Galet");
        var ocean = Palette("Océan");
        await state.SelectThemeAsync(galet, TestContext.Current.CancellationToken);

        await state.SelectPaletteAsync(ocean, TestContext.Current.CancellationToken);

        Assert.Same(galet, state.Theme);
        Assert.Same(ocean, state.Palette);
        Assert.Same(ocean, appearance.Palette);
        Assert.False(state.HasThemePalette);
        Assert.Equal(galet.With(ocean).Light, state.Light);
        Assert.Equal(galet.With(ocean).Dark, state.Dark);
    }

    [Fact]
    public async Task ResetPalette_ReturnsToThePaletteOfTheTheme()
    {
        var js = new RecordingJsRuntime();
        var (state, appearance) = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        await state.SelectThemeAsync(Theme("Néon"), TestContext.Current.CancellationToken);
        await state.SelectPaletteAsync(Palette("Forêt"), TestContext.Current.CancellationToken);

        await state.ResetPaletteAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Néon", state.Theme.Name);
        Assert.Equal("Électrique", state.Palette.Name);
        Assert.True(state.HasThemePalette);
        Assert.Null(appearance.Palette);
    }

    [Fact]
    public async Task SelectBackdrop_OfTheThemesOwn_KeepsNoChoice()
    {
        var js = new RecordingJsRuntime();
        var (state, appearance) = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        await state.SelectThemeAsync(Theme("Givre"), TestContext.Current.CancellationToken);

        await state.SelectBackdropAsync(OmniThemeBackdrops.None, TestContext.Current.CancellationToken);
        Assert.Same(OmniThemeBackdrops.None, appearance.Backdrop);

        await state.SelectBackdropAsync(OmniThemePresets.DefaultBackdropFor(state.Theme), TestContext.Current.CancellationToken);
        Assert.Null(appearance.Backdrop);
        Assert.Same(OmniThemePresets.DefaultBackdropFor(state.Theme), state.DrawnBackdrop);
        Assert.True(state.MovesBackdrop);

        await state.SelectBackdropAsync(OmniThemeBackdrops.None, TestContext.Current.CancellationToken);
        await state.SelectBackdropAsync(null, TestContext.Current.CancellationToken);
        Assert.Null(appearance.Backdrop);
    }

    [Fact]
    public async Task Set_KeepsOnlyTheTokensMovedAwayFromTheValueInForce()
    {
        var js = new RecordingJsRuntime();
        var (state, _) = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        var accent = state.Tokens.Single(token => token.Name == "--omni-color-accent");
        var inForce = state.ValueOf(accent);

        await state.SetAsync(accent, "#ff0000", TestContext.Current.CancellationToken);
        Assert.Equal("#ff0000", state.ValueOf(accent));
        Assert.Equal("#ff0000", state.Light["--omni-color-accent"]);
        Assert.Equal("#ff0000", state.Dark["--omni-color-accent"]);
        Assert.Equal("#ff0000", JsonSerializer.Deserialize<Dictionary<string, string>>((string)js.Calls[^1].Arguments[4]!)!["--omni-color-accent"]);

        await state.SetAsync(accent, inForce, TestContext.Current.CancellationToken);
        Assert.Empty(state.Edits);
        Assert.Equal(inForce, state.ValueOf(accent));
    }

    [Fact]
    public async Task ChoosingAnotherThemeOrPalette_DropsTheEdits()
    {
        var js = new RecordingJsRuntime();
        var (state, _) = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        var accent = state.Tokens.Single(token => token.Name == "--omni-color-accent");

        await state.SetAsync(accent, "#123456", TestContext.Current.CancellationToken);
        await state.SelectPaletteAsync(Palette("Prune"), TestContext.Current.CancellationToken);
        Assert.Empty(state.Edits);

        await state.SetAsync(accent, "#123456", TestContext.Current.CancellationToken);
        await state.SelectThemeAsync(Theme("Octet"), TestContext.Current.CancellationToken);
        Assert.Empty(state.Edits);

        // Reset all of the appearance window picks the first theme again even when it is in force: the edits go too.
        await state.SelectThemeAsync(OmniThemePresets.All[0], TestContext.Current.CancellationToken);
        await state.SetAsync(accent, "#123456", TestContext.Current.CancellationToken);
        await state.SelectThemeAsync(OmniThemePresets.All[0], TestContext.Current.CancellationToken);
        Assert.Empty(state.Edits);
    }

    [Fact]
    public async Task SetMode_SendsBothHalvesAndTheModeAndKeepsTheEdits()
    {
        var js = new RecordingJsRuntime();
        var (state, _) = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        var accent = state.Tokens.Single(token => token.Name == "--omni-color-accent");
        await state.SetAsync(accent, "#abcdef", TestContext.Current.CancellationToken);

        await state.SetModeAsync(OmniAppearance.Dark, TestContext.Current.CancellationToken);
        Assert.Equal(OmniAppearance.Dark, state.Mode);
        Assert.Equal("dark", js.Calls[^1].Arguments[2]);
        Assert.Equal("#abcdef", state.Edits["--omni-color-accent"]);
        Assert.Equal(state.Dark["--omni-color-surface"], Half(js.Calls[^1].Arguments[1])["--omni-color-surface"]);
        Assert.Equal("#abcdef", state.ValueOf(accent));

        await state.SetModeAsync(OmniAppearance.System, TestContext.Current.CancellationToken);
        Assert.Equal("system", js.Calls[^1].Arguments[2]);
    }

    [Fact]
    public async Task TheFont_IsLaidOverBothHalves_AndTheLevelsAreClamped()
    {
        var js = new RecordingJsRuntime();
        var (state, _) = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        var font = OmniThemeFonts.All[^1];

        await state.SetFontAsync(font, TestContext.Current.CancellationToken);
        await state.SetTextSizeLevelAsync(99, TestContext.Current.CancellationToken);
        await state.SetControlSizeLevelAsync(-4, TestContext.Current.CancellationToken);

        Assert.Equal(font.Family, state.Painted.Light["--omni-font-family"]);
        Assert.Equal(font.Family, state.Painted.Dark["--omni-heading-font-family"]);
        Assert.Equal(10, state.TextSizeLevel);
        Assert.Equal(1, state.ControlSizeLevel);
    }

    [Fact]
    public async Task Reset_DropsTheEditsAndKeepsTheCombination()
    {
        var js = new RecordingJsRuntime();
        var (state, _) = StateOver(js);
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
    /// PLAN-004 lot 9, Contrôle: a theme exported with a palette that is not its own carries the
    /// palette's colours and the theme's shape, in both halves.
    /// </summary>
    [Fact]
    public async Task Export_OfAThemeWithAForeignPalette_CarriesThePaletteColoursAndTheThemeShape()
    {
        var js = new RecordingJsRuntime();
        var (state, _) = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        var galet = Theme("Galet");
        var ocean = Palette("Océan");
        Assert.NotEqual("Océan", OmniThemePresets.DefaultPaletteFor(galet).Name);
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
        var (state, _) = StateOver(js);
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
        var (state, _) = StateOver(js);
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
        var (state, _) = StateOver(js);
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
        var (state, _) = StateOver(js);
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
        var (state, _) = StateOver(js);
        var announced = 0;
        state.Changed += () => announced++;

        await state.InitializeAsync(TestContext.Current.CancellationToken);
        await state.SelectThemeAsync(Theme("Rétro"), TestContext.Current.CancellationToken);
        await state.SelectPaletteAsync(Palette("Mono"), TestContext.Current.CancellationToken);
        await state.SetModeAsync(OmniAppearance.Dark, TestContext.Current.CancellationToken);
        await state.SetDensityAsync(OmniDensity.Compact, TestContext.Current.CancellationToken);
        await state.ResetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(6, announced);
    }

    /// <summary>The level the package draws as shipped, read from the package's single source.</summary>
    private const int AppearanceLookDefaultLevel = Internal.AppearanceLook.DefaultLevel;

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

    /// <summary>The showcase state over a package appearance state whose preference lives in <paramref name="browser"/>.</summary>
    private static (ThemeState State, OmniAppearanceState Appearance) StateOver(RecordingJsRuntime js, PreferenceBrowser? browser = null)
    {
        var preferences = new OmniPreferences(
            browser ?? new PreferenceBrowser(),
            null,
            new OmniPreferencesOptions { SyncInterval = Timeout.InfiniteTimeSpan },
            NullLogger<OmniPreferences>.Instance,
            TimeProvider.System);
        var appearance = new OmniAppearanceState(preferences, js, NullLogger<OmniAppearanceState>.Instance);
        var http = new HttpClient(new StubHandler(Css)) { BaseAddress = new Uri("https://localhost/") };
        return (new ThemeState(new ThemeTokenReader(http), js, appearance), appearance);
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
