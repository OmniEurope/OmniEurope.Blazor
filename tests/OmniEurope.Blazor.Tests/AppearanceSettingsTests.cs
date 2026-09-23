using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class AppearanceSettingsTests : OmniBunitContext
{
    [Fact]
    public void Theme_picker_has_one_default_choice()
    {
        var settings = Render<OmniAppearanceSettings>();
        var options = settings.Find("select[aria-label='Thème']").QuerySelectorAll("option");

        Assert.Equal(10, options.Length);
        Assert.Single(options, option => option.TextContent == "Essentiel (défaut)");
        Assert.DoesNotContain(options, option => option.TextContent.Contains("paquet", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("Ardoise", "Océan")]
    [InlineData("Galet", "Forêt")]
    public void Theme_palette_is_named_in_the_selector(string themeName, string paletteName)
    {
        var theme = OmniThemePresets.All.Single(item => item.Name == themeName);
        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.Preset, theme));

        Assert.Contains($"{paletteName} (défaut)", settings.Find("select[aria-label='Palette']").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Default_buttons_restore_levels()
    {
        int? textSize = null;
        int? density = null;
        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.TextSizeLevel, 8)
            .Add(component => component.DensityLevel, 2)
            .Add(component => component.TextSizeLevelChanged, value => textSize = value)
            .Add(component => component.DensityLevelChanged, value => density = value));

        settings.FindAll(".omni-appearance-settings__row")[3].QuerySelector("button")!.Click();
        var rows = settings.FindAll(".omni-appearance-settings--scale .omni-appearance-settings__row");
        rows[0].QuerySelectorAll("button").Last().Click();
        rows[1].QuerySelectorAll("button").Last().Click();

        Assert.Equal(5, textSize);
        Assert.Equal(5, density);
    }
}
