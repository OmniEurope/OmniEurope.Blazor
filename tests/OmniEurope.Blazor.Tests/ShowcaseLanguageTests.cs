using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
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
/// The showcase in the 24 official EU languages: the header selector, the culture the page runs in,
/// and the resources of every language.
/// </summary>
public sealed partial class ShowcaseLanguageTests : OmniBunitContext
{
    /// <summary>The 24 official languages of the Union, as the plan lists them (PLAN-007).</summary>
    private static readonly string[] EuLanguages =
    [
        "fr", "en", "bg", "cs", "da", "de", "el", "es", "et", "fi", "ga", "hr",
        "hu", "it", "lt", "lv", "mt", "nl", "pl", "pt", "ro", "sk", "sl", "sv"
    ];

    [Fact]
    public void Languages_AreTheTwentyFourOfficialEuLanguagesInProtocolOrder()
    {
        Assert.Equal(EuLanguages.Order(StringComparer.Ordinal), ShowcaseLanguages.All.Select(language => language.Code).Order(StringComparer.Ordinal));

        // The EU protocol order: each language by its own name, Latin transliteration for Bulgarian
        // (Balgarski) and Greek (Elliniká), as the institutions' language menus list them.
        Assert.Equal(
            ["bg", "es", "cs", "da", "de", "et", "el", "en", "fr", "ga", "hr", "it", "lv", "lt", "hu", "mt", "nl", "pl", "pt", "ro", "sk", "sl", "fi", "sv"],
            ShowcaseLanguages.All.Select(language => language.Code));
        Assert.All(ShowcaseLanguages.All, language =>
            Assert.Equal(language.Code, CultureInfo.GetCultureInfo(language.Culture).TwoLetterISOLanguageName));
    }

    [Theory]
    [InlineData(null, "fr")]
    [InlineData("", "fr")]
    [InlineData("xx", "fr")]
    [InlineData("pt-BR", "fr")]
    [InlineData("de", "de")]
    [InlineData("EL", "el")]
    [InlineData(" ga ", "ga")]
    public void Resolve_FallsBackToFrenchForAnUnknownOrMissingCode(string? saved, string expected) =>
        Assert.Equal(expected, ShowcaseLanguages.Resolve(saved).Code);

    [Theory]
    [InlineData("fr")]
    [InlineData("cs")]
    [InlineData("pl")]
    [InlineData("sv")]
    [InlineData("bg")]
    public void FormattingCulture_KeepsTheEuroForTheSampleAmounts(string code)
    {
        var culture = ShowcaseLanguages.FormattingCultureOf(ShowcaseLanguages.Resolve(code));

        Assert.Contains("€", 1234m.ToString("C0", culture), StringComparison.Ordinal);
        Assert.Equal(code, culture.TwoLetterISOLanguageName);
    }

    [Fact]
    public void Selector_ListsTheTwentyFourLanguagesByTheirOwnNameWithAnAccessibleName()
    {
        var selector = Render<LanguageSelector>();

        var select = selector.Find("select.showcase-language");
        var options = select.QuerySelectorAll("option").Select(option => option.TextContent).ToArray();
        Assert.Equal(24, options.Length);
        Assert.Equal(ShowcaseLanguages.All.Select(language => language.Endonym), options);
        Assert.Contains("Deutsch", options);
        Assert.Contains("Ελληνικά", options);
        Assert.Contains("Gaeilge", options);
        Assert.Contains("Malti", options);
        Assert.Equal("Langue de la vitrine", select.GetAttribute("aria-label"));
        Assert.Equal("Français", select.QuerySelector("option[selected]")?.TextContent);
    }

    [Fact]
    public void Selector_SavesTheChosenLanguageAndReloadsThePage()
    {
        JSInterop.Setup<bool>("omniShowcaseCulture.save", ShowcaseLanguages.StorageKey, "de").SetResult(true);
        var navigation = (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();
        var selector = Render<LanguageSelector>();

        selector.Find("select").Change(IndexOf("de"));

        Assert.Single(JSInterop.Invocations, invocation => invocation.Identifier == "omniShowcaseCulture.save");
        var reload = Assert.Single(navigation.History);
        Assert.True(reload.Options.ForceLoad);
        Assert.Equal(navigation.Uri, navigation.ToAbsoluteUri(reload.Uri).ToString());
    }

    [Fact]
    public void Selector_StaysOnTheCurrentLanguageWhenTheBrowserRefusesToStoreTheChoice()
    {
        JSInterop.Setup<bool>("omniShowcaseCulture.save", ShowcaseLanguages.StorageKey, "de").SetResult(false);
        var navigation = (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();
        var selector = Render<LanguageSelector>();

        selector.Find("select").Change(IndexOf("de"));

        Assert.Empty(navigation.History);
        Assert.Equal("Français", selector.Find("select option[selected]").TextContent);
    }

    [Fact]
    public void Resources_ExistForEveryLanguageWithTheNeutralKeysAndPlaceholders()
    {
        // Red until every ShowcaseStrings.<code>.resx is translated with the neutral file's keys and
        // placeholders: the parity is the proof the selector's 24 entries are not French in disguise.
        var neutral = Resource("ShowcaseStrings.resx");
        var missing = EuLanguages.Where(code => code != "fr" && !File.Exists(ResourcePath($"ShowcaseStrings.{code}.resx"))).ToArray();
        Assert.True(missing.Length == 0, $"Missing showcase translations: {string.Join(", ", missing)}.");

        foreach (var code in EuLanguages.Where(code => code != "fr"))
        {
            var translated = Resource($"ShowcaseStrings.{code}.resx");
            Assert.True(neutral.Keys.Order(StringComparer.Ordinal).SequenceEqual(translated.Keys.Order(StringComparer.Ordinal)),
                $"ShowcaseStrings.{code}.resx: missing [{string.Join(", ", neutral.Keys.Except(translated.Keys))}], extra [{string.Join(", ", translated.Keys.Except(neutral.Keys))}].");
            var drifted = neutral.Where(entry => Placeholders(entry.Value) != Placeholders(translated[entry.Key])).Select(entry => entry.Key).ToArray();
            Assert.True(drifted.Length == 0, $"ShowcaseStrings.{code}.resx: placeholders differ in {string.Join(", ", drifted)}.");
        }
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("el-GR")]
    [InlineData("cs-CZ")]
    [InlineData("en-IE")]
    public void Resources_ResolveInTheChosenLanguage(string cultureName)
    {
        var previous = CultureInfo.CurrentUICulture;
        var localizer = Services.GetRequiredService<IStringLocalizer<ShowcaseStrings>>();
        var french = localizer["HeaderBadge"].Value;
        var fileName = $"ShowcaseStrings.{CultureInfo.GetCultureInfo(cultureName).TwoLetterISOLanguageName}.resx";
        Assert.True(File.Exists(ResourcePath(fileName)), $"{fileName} is not translated yet.");
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);

            var translated = localizer["HeaderBadge"];

            Assert.False(translated.ResourceNotFound);
            Assert.Equal(Resource(fileName)["HeaderBadge"], translated.Value);
            Assert.NotEqual(french, translated.Value);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Gallery_CountsThemesAndPalettesFromTheLibraryInsteadOfSpellingThemOut()
    {
        var themes = OmniThemePresets.All.Count;
        var palettes = OmniThemePalettes.All.Count;

        var gallery = Render<OmniEurope.Blazor.Showcase.Components.Pages.ComponentGallery>(parameters => parameters
            .Add(page => page.DemoKey, "themes"));

        Assert.Contains(
            $"<li>{themes} thèmes et {palettes} palettes, soit {themes * palettes} combinaisons, chacune en clair et en sombre.</li>",
            gallery.Markup,
            StringComparison.Ordinal);
    }

    private static string IndexOf(string code) =>
        ShowcaseLanguages.All.ToList().FindIndex(language => language.Code == code).ToString(CultureInfo.InvariantCulture);

    private static string Placeholders(string value) =>
        string.Join(",", PlaceholderPattern().Matches(value).Select(match => match.Groups[1].Value).Distinct().Order(StringComparer.Ordinal));

    private static Dictionary<string, string> Resource(string fileName) => XDocument.Load(ResourcePath(fileName))
        .Root!
        .Elements("data")
        .ToDictionary(element => (string)element.Attribute("name")!, element => (string?)element.Element("value") ?? string.Empty, StringComparer.Ordinal);

    private static string ResourcePath(string fileName) =>
        Path.Combine(Root, "site", "OmniEurope.Blazor.Showcase", "Resources", fileName);

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OmniEurope.Blazor.slnx")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
        }
    }

    [GeneratedRegex(@"\{(\d+)(?:[:,][^}]*)?\}")]
    private static partial Regex PlaceholderPattern();
}
