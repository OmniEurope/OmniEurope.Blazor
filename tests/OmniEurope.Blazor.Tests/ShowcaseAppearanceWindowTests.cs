using System.Net;
using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;
using OmniEurope.Blazor.Showcase.Components.Layout;
using OmniEurope.Blazor.Showcase.Localization;
using OmniEurope.Blazor.Showcase.Theming;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The four settings the showcase's appearance window gained (font, text size, control size, content width):
/// they are kept as the package appearance preference and replayed, bad values left under the former showcase entry fall back to the defaults,
/// the layout binds all four so the window shows the Text and Layout sections, and the content width reaches
/// the main area. The browser effect of the levels (attributes on the document root) is checked in a real browser.
/// </summary>
public sealed class ShowcaseAppearanceWindowTests : OmniBunitContext
{
    private const string Css = ":root { --omni-color-accent: #2563eb; }";

    public ShowcaseAppearanceWindowTests()
    {
        Services.AddScoped(_ => new HttpClient(new StylesheetResponder(Css)) { BaseAddress = new Uri("https://localhost/") });
        Services.AddScoped<ThemeTokenReader>();
        Services.AddScoped<ThemeState>();
        Services.AddScoped<ShowcaseLanguageSwitcher>();
    }

    [Fact]
    public async Task TheFourSettings_AreStored_AndReplayedByTheNextVisit()
    {
        var js = new ShowcaseThemeJs();
        var browser = new PreferenceBrowser();
        var state = StateOver(js, browser);
        await state.InitializeAsync(Xunit.TestContext.Current.CancellationToken);
        var font = OmniThemeFonts.All[^1];

        await state.SetFontAsync(font, Xunit.TestContext.Current.CancellationToken);
        await state.SetTextSizeLevelAsync(8, Xunit.TestContext.Current.CancellationToken);
        await state.SetControlSizeLevelAsync(2, Xunit.TestContext.Current.CancellationToken);
        await state.SetFullWidthAsync(true, Xunit.TestContext.Current.CancellationToken);

        var apply = js.Calls.Last(call => call.Identifier == "omniShowcaseTheme.apply");
        Assert.Equal(8, apply.Arguments[5]);
        Assert.Equal(2, apply.Arguments[6]);
        // The four settings are the package state's preference; the showcase entry keeps the hand edits alone.
        var serialized = (string)apply.Arguments[4]!;
        Assert.Empty(JsonSerializer.Deserialize<Dictionary<string, string>>(serialized)!);
        Assert.Contains(browser.Storage.Values, entry => entry.Contains(font.Name, StringComparison.Ordinal));
        Assert.Equal(font.Family, state.Painted.Light["--omni-font-family"]);

        var next = StateOver(new ShowcaseThemeJs { Stored = serialized }, browser);
        await next.InitializeAsync(Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(font, next.Font);
        Assert.Equal(8, next.TextSizeLevel);
        Assert.Equal(2, next.ControlSizeLevel);
        Assert.True(next.FullWidth);
    }

    [Theory]
    [InlineData("{\"font\":\"Nope\",\"textSize\":\"abc\",\"controlSize\":\"11\",\"fullWidth\":\"maybe\"}")]
    [InlineData("{\"textSize\":\"0\",\"controlSize\":\"-3\"}")]
    [InlineData("{\"font\":\"\",\"textSize\":\"\",\"controlSize\":\"\",\"fullWidth\":\"\"}")]
    [InlineData("{ not json")]
    public async Task GarbageInTheStore_FallsBackToTheDefaults(string stored)
    {
        var state = StateOver(new ShowcaseThemeJs { Stored = stored });

        await state.InitializeAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Null(state.Font);
        Assert.Equal(AppearanceLook.DefaultLevel, state.TextSizeLevel);
        Assert.Equal(AppearanceLook.DefaultLevel, state.ControlSizeLevel);
        Assert.False(state.FullWidth);
    }

    [Fact]
    public async Task SettingTheDefaultsAgain_RestoresThem_AndALevelOutOfRangeIsClamped()
    {
        var js = new ShowcaseThemeJs();
        var state = StateOver(js);
        await state.InitializeAsync(Xunit.TestContext.Current.CancellationToken);
        await state.SetFontAsync(OmniThemeFonts.All[0], Xunit.TestContext.Current.CancellationToken);
        await state.SetTextSizeLevelAsync(99, Xunit.TestContext.Current.CancellationToken);
        await state.SetControlSizeLevelAsync(-4, Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(10, state.TextSizeLevel);
        Assert.Equal(1, state.ControlSizeLevel);

        await state.SetFontAsync(null, Xunit.TestContext.Current.CancellationToken);
        await state.SetTextSizeLevelAsync(5, Xunit.TestContext.Current.CancellationToken);
        await state.SetControlSizeLevelAsync(5, Xunit.TestContext.Current.CancellationToken);
        await state.SetFullWidthAsync(false, Xunit.TestContext.Current.CancellationToken);

        Assert.Null(state.Font);
        Assert.Equal(5, state.TextSizeLevel);
        Assert.Equal(5, state.ControlSizeLevel);
        Assert.False(state.FullWidth);
        Assert.Equal(
            OmniThemePresets.All[0].Light.GetValueOrDefault("--omni-font-family"),
            state.Painted.Light.GetValueOrDefault("--omni-font-family"));
    }

    [Fact]
    public void TheLayoutBindsFontTextSizeControlSizeAndWidth_SoTheWindowShowsTheTextAndLayoutSections()
    {
        var layout = Render<MainLayout>(parameters => parameters.Add(l => l.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<p>page</p>"))));
        var state = Services.GetRequiredService<ThemeState>();
        layout.Find("button.omni-app-menu__trigger").Click();
        layout.Find(".omni-app-menu__theme").Click();

        layout.WaitForAssertion(() => Assert.NotEmpty(layout.FindAll(".omni-appearance-window")));
        var window = layout.Find(".omni-appearance-window");
        Assert.Equal(3, window.QuerySelectorAll(".omni-appearance-window__heading").Length);
        // Mode, theme, palette, backdrop, motion; font; text size; density, control size, width.
        Assert.True(window.QuerySelectorAll(".omni-appearance-settings__row").Length >= 9);

        var fonts = window.QuerySelectorAll("select").Single(select => select.QuerySelectorAll("option").Length == OmniThemeFonts.All.Count);
        var target = OmniThemeFonts.All.First(font => font != OmniThemePresets.DefaultFontFor(OmniThemePresets.All[0]));
        fonts.Change(fonts.QuerySelectorAll("option").Single(option => option.TextContent.StartsWith(target.Name, StringComparison.Ordinal)).GetAttribute("value")!);
        Assert.Equal(target, state.Font);

        Assert.Contains("omni-main--wide", layout.Find("main.omni-main").ClassName, StringComparison.Ordinal);
        layout.InvokeAsync(() => state.SetFullWidthAsync(true));
        layout.WaitForAssertion(() => Assert.DoesNotContain("omni-main--wide", layout.Find("main.omni-main").ClassName, StringComparison.Ordinal));
    }

    private static ThemeState StateOver(ShowcaseThemeJs js, PreferenceBrowser? browser = null) => ShowcaseThemeStateSetup.Create(js, Css, browser).State;
}
