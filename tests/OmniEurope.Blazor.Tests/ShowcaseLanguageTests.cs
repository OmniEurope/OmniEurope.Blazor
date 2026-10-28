using System.Globalization;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Showcase.Components.Gallery.Foundations;
using OmniEurope.Blazor.Showcase.Localization;
using OmniEurope.Blazor.Showcase.Resources;
using OmniEurope.Blazor.Showcase.Theming;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The showcase in English (the neutral resources, the default) and French: the language switcher, the
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
    public void Switcher_OffersBothLanguagesByTheirOwnName_AndNamesTheCurrentOne()
    {
        WithUiCulture("en-IE", () =>
        {
            Assert.Equal(["English", "Français"], ShowcaseLanguageSwitcher.Languages.Select(language => language.Name));
            Assert.Equal(["en", "fr"], ShowcaseLanguageSwitcher.Languages.Select(language => language.Code));
            Assert.Equal("en", ShowcaseLanguageSwitcher.Current);
        });
    }

    [Fact]
    public void Switcher_SavesTheChosenLanguageAndReloadsThePage()
    {
        WithUiCulture("en-IE", () =>
        {
            JSInterop.Setup<bool>("omniShowcaseCulture.save", ShowcaseLanguages.StorageKey, "fr").SetResult(true);
            var navigation = (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();
            var switcher = new ShowcaseLanguageSwitcher(JSInterop.JSRuntime, navigation);

            var reloaded = switcher.SwitchAsync("fr").GetAwaiter().GetResult();

            Assert.True(reloaded);
            Assert.Single(JSInterop.Invocations, invocation => invocation.Identifier == "omniShowcaseCulture.save");
            var reload = Assert.Single(navigation.History);
            Assert.True(reload.Options.ForceLoad);
            Assert.Equal(navigation.Uri, navigation.ToAbsoluteUri(reload.Uri).ToString());
        });
    }

    [Fact]
    public void Switcher_StaysOnTheCurrentLanguageWhenTheBrowserRefusesToStoreTheChoice()
    {
        WithUiCulture("en-IE", () =>
        {
            JSInterop.Setup<bool>("omniShowcaseCulture.save", ShowcaseLanguages.StorageKey, "fr").SetResult(false);
            var navigation = (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();
            var switcher = new ShowcaseLanguageSwitcher(JSInterop.JSRuntime, navigation);

            var reloaded = switcher.SwitchAsync("fr").GetAwaiter().GetResult();

            Assert.False(reloaded);
            Assert.Empty(navigation.History);
            Assert.Equal("en", ShowcaseLanguageSwitcher.Current);
        });
    }

    [Fact]
    public void Switcher_DoesNothingWhenTheChosenLanguageIsTheCurrentOne()
    {
        WithUiCulture("fr-FR", () =>
        {
            var navigation = (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();
            var switcher = new ShowcaseLanguageSwitcher(JSInterop.JSRuntime, navigation);

            var reloaded = switcher.SwitchAsync("fr").GetAwaiter().GetResult();

            Assert.False(reloaded);
            Assert.Empty(navigation.History);
            Assert.DoesNotContain(JSInterop.Invocations, invocation => invocation.Identifier == "omniShowcaseCulture.save");
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

        Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("https://localhost/") });
        Services.AddScoped<ThemeTokenReader>();
        Services.AddScoped<ThemeState>();

        WithUiCulture("en-IE", () =>
        {
            var page = Render<ThemePage>();

            Assert.Contains(
                $"{themes} themes and {palettes} palettes, {themes * palettes} combinations, each in light and dark.",
                page.Markup,
                StringComparison.Ordinal);
        });
    }

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
