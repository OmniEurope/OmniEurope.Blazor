using System.Globalization;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The descriptions of the shipped themes, palettes, backdrops and fonts are package texts: read in the current UI
/// culture, while the names stay as shipped. A description a host writes itself, through the constructor or a
/// <c>with</c> expression, is the host's text and is never replaced.
/// </summary>
public sealed class ThemeDescriptionLocalizationTests
{
    [Fact]
    public void TheCatalogueDescriptions_FollowTheCurrentUiCulture_AndTheNamesStay()
    {
        var galet = OmniThemePresets.All.Single(theme => theme.Name == "Galet");
        var ocean = OmniThemePalettes.All.Single(palette => palette.Name == "Océan");
        var inter = OmniThemeFonts.All.Single(font => font.Name == "Inter");

        using (Culture("en-IE"))
        {
            Assert.Equal("Rounded throughout, pill buttons, a fine edge and a soft shadow instead of a border.", galet.Description);
            Assert.Equal("Bright blue on a sea-foam background, navy night in dark mode.", ocean.Description);
            Assert.Equal("Plain page, no backdrop drawn behind the content.", OmniThemeBackdrops.None.Description);
            Assert.Equal("Neutral sans-serif designed for screens (free web font).", inter.Description);
            Assert.Equal("Océan", ocean.Name);
        }

        using (Culture("fr-FR"))
        {
            Assert.Equal("Tout en rondeur, boutons pilule, liseré et ombre diffuse à la place de la bordure.", galet.Description);
            Assert.Equal("Bleu franc sur fond d'écume, nuit marine en sombre.", ocean.Description);
            Assert.Equal("Sans empattement neutre dessinée pour les écrans (police web libre).", inter.Description);
        }
    }

    /// <summary>Every shipped entry has its own text in another language than the neutral French one.</summary>
    [Fact]
    public void EveryCatalogueEntry_IsTranslated()
    {
        var descriptions = new Func<IEnumerable<string>>[]
        {
            () => OmniThemePresets.All.Select(theme => theme.Description),
            () => OmniThemePalettes.All.Select(palette => palette.Description),
            () => OmniThemeBackdrops.All.Select(backdrop => backdrop.Description),
            () => OmniThemeFonts.All.Select(font => font.Description)
        };

        foreach (var read in descriptions)
        {
            string[] french;
            string[] german;
            using (Culture("fr-FR"))
            {
                french = [.. read()];
            }

            using (Culture("de-DE"))
            {
                german = [.. read()];
            }

            Assert.All(german, text => Assert.False(string.IsNullOrWhiteSpace(text)));
            Assert.All(french.Zip(german), pair => Assert.NotEqual(pair.First, pair.Second));
        }
    }

    /// <summary>A preset painted with a palette and a backdrop keeps the theme's translated description.</summary>
    [Fact]
    public void APaintedPreset_KeepsTheTranslatedDescriptionOfItsTheme()
    {
        var galet = OmniThemePresets.All.Single(theme => theme.Name == "Galet");
        var painted = galet.With(OmniThemePalettes.All[^1]).With(OmniThemeBackdrops.None);

        using (Culture("en-IE"))
        {
            Assert.Equal(galet.Description, painted.Description);
        }
    }

    [Fact]
    public void ADescriptionWrittenByTheHost_IsKeptInEveryCulture()
    {
        var theme = OmniThemePresets.All[0] with { Description = "Custom theme" };
        var palette = OmniThemePalettes.All[0] with { Description = "Custom palette" };
        var backdrop = OmniThemeBackdrops.None with { Description = "Custom backdrop" };
        var font = OmniThemeFonts.All[0] with { Description = "Custom font" };
        var built = new OmniThemeFont("Mine", "Built by the host", "serif");

        using (Culture("de-DE"))
        {
            Assert.Equal("Custom theme", theme.Description);
            Assert.Equal("Custom palette", palette.Description);
            Assert.Equal("Custom backdrop", backdrop.Description);
            Assert.Equal("Custom font", font.Description);
            Assert.Equal("Built by the host", built.Description);
        }

        Assert.Null(theme.DescriptionKey);
        Assert.Null(palette.DescriptionKey);
        Assert.Null(backdrop.DescriptionKey);
        Assert.Null(font.DescriptionKey);
        Assert.Null(built.DescriptionKey);
        Assert.NotEqual(OmniThemeBackdrops.None, backdrop);
    }

    [Fact]
    public void AKeyThePackageDoesNotHave_ReadsTheTextGiven_AndCannotBeTheNeutralText()
    {
        Assert.Equal("kept", CatalogDescriptions.Describe("NoSuchDescription", "kept"));
        Assert.Throws<InvalidOperationException>(() => CatalogDescriptions.Neutral("NoSuchDescription"));
    }

    private static IDisposable Culture(string name)
    {
        var previous = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
        return new Restore(previous.CurrentCulture, previous.CurrentUICulture);
    }

    private sealed class Restore(CultureInfo culture, CultureInfo uiCulture) : IDisposable
    {
        public void Dispose() => (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = (culture, uiCulture);
    }
}
