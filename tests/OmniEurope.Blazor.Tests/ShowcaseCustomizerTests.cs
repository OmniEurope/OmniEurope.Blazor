using OmniEurope.Blazor.Components;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Showcase.Components.Pages;
using OmniEurope.Blazor.Showcase.Theming;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Renders the theme customizer and drives its control bar.
/// </summary>
/// <remarks>
/// The gallery pages are covered by rendering every demonstration; this page is not in that
/// catalogue, so without this it would be the only screen of the site nothing renders. It also
/// proves the theme and palette catalogues reach the markup, and that the two pickers behave as
/// the plan asks: a theme comes with its own palette, a palette leaves the theme alone.
/// </remarks>
public sealed class ShowcaseCustomizerTests : OmniBunitContext
{
    public ShowcaseCustomizerTests()
    {
        Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("https://localhost/") });
        Services.AddScoped<ThemeTokenReader>();
        Services.AddScoped<ThemeState>();
    }

    [Fact]
    public void Customizer_Renders()
    {
        var page = Render<Customizer>();

        Assert.False(string.IsNullOrWhiteSpace(page.Markup));
        Assert.Contains("customize-title", page.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Customizer_OffersEveryThemeAndEveryPaletteOfTheCatalogue()
    {
        var page = Render<Customizer>();

        var themes = page.FindAll("#workshop-theme option").Select(option => option.TextContent).ToArray();
        var palettes = page.FindAll("#workshop-palette option").Select(option => option.TextContent).ToArray();
        Assert.Equal(OmniThemePresets.All.Select(theme => theme.Name), themes);
        Assert.Equal(OmniThemePalettes.All.Select(palette => palette.Name), palettes);
    }

    [Fact]
    public void PickingATheme_PaintsItWithItsOwnPalette()
    {
        var page = Render<Customizer>();
        var state = Services.GetRequiredService<ThemeState>();
        page.Find("#workshop-palette").Change(PaletteIndex("Mono"));
        Assert.Equal("Mono", state.Palette.Name);

        page.Find("#workshop-theme").Change(ThemeIndex("Galet"));

        Assert.Equal("Galet", state.Theme.Name);
        Assert.Equal("Forêt", state.Palette.Name);
        Assert.True(page.Find("#workshop-reset-palette").HasAttribute("disabled"));
    }

    [Fact]
    public void PickingAPalette_LeavesTheThemeAlone_AndDefaultBringsItsPaletteBack()
    {
        var page = Render<Customizer>();
        var state = Services.GetRequiredService<ThemeState>();
        page.Find("#workshop-theme").Change(ThemeIndex("Néon"));

        page.Find("#workshop-palette").Change(PaletteIndex("Forêt"));

        Assert.Equal("Néon", state.Theme.Name);
        Assert.Equal("Forêt", state.Palette.Name);
        Assert.False(page.Find("#workshop-reset-palette").HasAttribute("disabled"));

        page.Find("#workshop-reset-palette").Click();

        Assert.Equal("Néon", state.Theme.Name);
        Assert.Equal("Électrique", state.Palette.Name);
    }

    [Fact]
    public void TheControlBar_SetsTheModeAndTheDensity()
    {
        var page = Render<Customizer>();
        var state = Services.GetRequiredService<ThemeState>();

        // The options follow OmniDensity: compact, comfortable, spacious.
        page.Find("#workshop-density").Change("2");
        // Light, dark, system: the options follow ThemeMode.
        page.FindAll("#workshop-mode [role=radio]")[2].Click();

        Assert.Equal(OmniDensity.Spacious, state.Density);
        Assert.Equal(ThemeMode.System, state.Mode);
    }

    /// <summary>The preview stacks the mockup's compositions, each with a density of its own on demand.</summary>
    [Fact]
    public void ThePreview_StacksTheCompositionsAndGivesASectionItsOwnDensity()
    {
        var page = Render<Customizer>();

        Assert.Equal(12, page.FindAll(".showcase-stage__block").Count);
        Assert.Empty(page.FindAll(".showcase-stage__block[data-omni-density]"));

        // Inherited, compact, comfortable, spacious.
        page.Find("#stage-formulaire-vertical-density").Change("1");

        var section = page.Find(".showcase-stage__block[data-omni-density]");
        Assert.Equal("compact", section.GetAttribute("data-omni-density"));
        Assert.Equal("stage-formulaire-vertical-title", section.GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void TheExport_ShowsTheCombinationInForce()
    {
        var page = Render<Customizer>();
        page.Find("#workshop-theme").Change(ThemeIndex("Papier"));
        page.Find("#workshop-palette").Change(PaletteIndex("Lagune"));

        var export = page.Find("#workshop-export").TextContent;

        Assert.Contains("theme Papier, palette Lagune", export, StringComparison.Ordinal);
        Assert.Contains("[data-omni-theme=\"dark\"] {", export, StringComparison.Ordinal);
    }

    [Fact]
    public void TheContrastRail_MeasuresEveryPairOfTheMockup()
    {
        var page = Render<Customizer>();

        Assert.Equal(ContrastAudit.Pairs.Count, page.FindAll(".showcase-contrast").Count);
        Assert.Equal(ContrastAudit.Swatches.Count, page.FindAll(".showcase-swatch").Count);
    }

    [Fact]
    public void Customizer_StaysWithinTheContentSecurityPolicy()
    {
        var page = Render<Customizer>();

        Assert.DoesNotContain("style=", page.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<style", page.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", page.Markup, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The drop-down posts the position of the option, in catalogue order.</summary>
    private static string ThemeIndex(string name) =>
        OmniThemePresets.All.Select((theme, index) => (theme, index)).Single(entry => entry.theme.Name == name).index.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static string PaletteIndex(string name) =>
        OmniThemePalettes.All.Select((palette, index) => (palette, index)).Single(entry => entry.palette.Name == name).index.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
