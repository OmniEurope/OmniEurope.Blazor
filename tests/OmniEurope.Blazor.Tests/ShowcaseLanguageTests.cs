using System.Globalization;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Showcase.Components.Layout;
using OmniEurope.Blazor.Showcase.Localization;
using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The showcase in English (the neutral resources, the default) and French: the header selector, the
/// culture the page runs in, and the resources each language resolves to. Owner decision (PLAN-005):
/// the language coverage of the resources is not tested; the selector and the cultures are.
/// </summary>
public sealed class ShowcaseLanguageTests : OmniBunitContext
{
    [Fact]
    public void Languages_AreEnglishThenFrench_EachInACultureOfAMemberState()
    {
        Assert.Equal(["en", "fr"], ShowcaseLanguages.All.Select(language => language.Code));
        Assert.Equal(["en-IE", "fr-FR"], ShowcaseLanguages.All.Select(language => language.Culture));
        Assert.Equal("en", ShowcaseLanguages.Default.Code);
        Assert.All(ShowcaseLanguages.All, language =>
            Assert.Equal(language.Code, CultureInfo.GetCultureInfo(language.Culture).TwoLetterISOLanguageName));
    }

    [Theory]
    [InlineData(null, "en")]
    [InlineData("", "en")]
    [InlineData("xx", "en")]
    [InlineData("de", "en")]
    [InlineData("pt-BR", "en")]
    [InlineData("fr", "fr")]
    [InlineData("FR", "fr")]
    [InlineData(" en ", "en")]
    public void Resolve_FallsBackToEnglishForAnUnknownOrMissingCode(string? saved, string expected) =>
        Assert.Equal(expected, ShowcaseLanguages.Resolve(saved).Code);

    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public void FormattingCulture_KeepsTheEuroForTheSampleAmounts(string code)
    {
        var culture = ShowcaseLanguages.FormattingCultureOf(ShowcaseLanguages.Resolve(code));

        Assert.Contains("€", 1234m.ToString("C0", culture), StringComparison.Ordinal);
        Assert.Equal(code, culture.TwoLetterISOLanguageName);
    }

    [Fact]
    public void Selector_ListsBothLanguagesByTheirOwnNameWithAnAccessibleName()
    {
        WithUiCulture("en-IE", () =>
        {
            var selector = Render<LanguageSelector>();

            var select = selector.Find("select.showcase-language");
            var options = select.QuerySelectorAll("option").Select(option => option.TextContent).ToArray();
            Assert.Equal(["English", "Français"], options);
            Assert.Equal("Showcase language", select.GetAttribute("aria-label"));
            Assert.Equal("English", select.QuerySelector("option[selected]")?.TextContent);
        });
    }

    [Fact]
    public void Selector_SavesTheChosenLanguageAndReloadsThePage()
    {
        WithUiCulture("en-IE", () =>
        {
            JSInterop.Setup<bool>("omniShowcaseCulture.save", ShowcaseLanguages.StorageKey, "fr").SetResult(true);
            var navigation = (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();
            var selector = Render<LanguageSelector>();

            selector.Find("select").Change(IndexOf("fr"));

            Assert.Single(JSInterop.Invocations, invocation => invocation.Identifier == "omniShowcaseCulture.save");
            var reload = Assert.Single(navigation.History);
            Assert.True(reload.Options.ForceLoad);
            Assert.Equal(navigation.Uri, navigation.ToAbsoluteUri(reload.Uri).ToString());
        });
    }

    [Fact]
    public void Selector_StaysOnTheCurrentLanguageWhenTheBrowserRefusesToStoreTheChoice()
    {
        WithUiCulture("en-IE", () =>
        {
            JSInterop.Setup<bool>("omniShowcaseCulture.save", ShowcaseLanguages.StorageKey, "fr").SetResult(false);
            var navigation = (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();
            var selector = Render<LanguageSelector>();

            selector.Find("select").Change(IndexOf("fr"));

            Assert.Empty(navigation.History);
            Assert.Equal("English", selector.Find("select option[selected]").TextContent);
        });
    }

    [Theory]
    [InlineData("en-IE", "Component library")]
    [InlineData("fr-FR", "Bibliothèque de composants")]
    public void Resources_ResolveInTheChosenLanguage(string cultureName, string expected)
    {
        WithUiCulture(cultureName, () =>
        {
            var translated = Services.GetRequiredService<IStringLocalizer<ShowcaseStrings>>()["HeaderBadge"];

            Assert.False(translated.ResourceNotFound);
            Assert.Equal(expected, translated.Value);
        });
    }

    [Fact]
    public void Gallery_CountsThemesAndPalettesFromTheLibraryInsteadOfSpellingThemOut()
    {
        var themes = OmniThemePresets.All.Count;
        var palettes = OmniThemePalettes.All.Count;

        WithUiCulture("en-IE", () =>
        {
            var gallery = Render<OmniEurope.Blazor.Showcase.Components.Pages.ComponentGallery>(parameters => parameters
                .Add(page => page.DemoKey, "themes"));

            Assert.Contains(
                $"<li>{themes} themes and {palettes} palettes, {themes * palettes} combinations, each in light and dark.</li>",
                gallery.Markup,
                StringComparison.Ordinal);
        });
    }

    private static string IndexOf(string code) =>
        ShowcaseLanguages.All.ToList().FindIndex(language => language.Code == code).ToString(CultureInfo.InvariantCulture);

    /// <summary>Runs an assertion under a UI culture, whatever the culture of the machine running the tests.</summary>
    internal static void WithUiCulture(string cultureName, Action action)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            action();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
