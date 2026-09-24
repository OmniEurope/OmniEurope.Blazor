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

        settings.FindAll(".omni-appearance-settings__row")[4].QuerySelector("button")!.Click();
        var rows = settings.FindAll(".omni-appearance-settings--scale .omni-appearance-settings__row");
        rows[0].QuerySelectorAll("button").Last().Click();
        rows[1].QuerySelectorAll("button").Last().Click();

        Assert.Equal(5, textSize);
        Assert.Equal(5, density);
    }

    [Fact]
    public void Scale_window_has_a_slider_under_each_level()
    {
        int? textSize = null;
        int? density = null;
        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.TextSizeLevelChanged, value => textSize = value)
            .Add(component => component.DensityLevelChanged, value => density = value));

        settings.FindAll(".omni-appearance-settings__row")[4].QuerySelector("button")!.Click();
        var sliders = settings.FindAll(".omni-appearance-settings--scale input[type=range]");
        Assert.Equal(2, sliders.Count);
        Assert.All(sliders, slider => Assert.Equal(("1", "10"), (slider.GetAttribute("min"), slider.GetAttribute("max"))));

        sliders[0].Input("8");
        settings.FindAll(".omni-appearance-settings--scale input[type=range]")[1].Input("3");

        Assert.Equal(8, textSize);
        Assert.Equal(3, density);
    }

    [Fact]
    public void Font_picker_marks_the_theme_font_as_default_and_theme_change_resets_palette_and_font()
    {
        var theme = OmniThemePresets.All.Single(item => item.Name == "Papier");
        OmniThemePreset? chosenTheme = null;
        var paletteReset = false;
        var fontReset = false;
        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.Preset, theme)
            .Add(component => component.Palette, OmniThemePalettes.All[0])
            .Add(component => component.Font, OmniThemeFonts.All.Single(font => font.Name == "JetBrains Mono"))
            .Add(component => component.PresetChanged, value => chosenTheme = value)
            .Add(component => component.PaletteChanged, value => paletteReset = value is null)
            .Add(component => component.FontChanged, value => fontReset = value is null));

        var options = settings.Find("select[aria-label='Police']").QuerySelectorAll("option");
        Assert.Equal(10, options.Length);
        Assert.Single(options, option => option.TextContent == "Source Serif (défaut)");
        Assert.Equal("Source Serif", OmniThemePresets.DefaultFontFor(theme).Name);

        // The drop-down posts the option index: Galet is the third theme.
        settings.Find("select[aria-label='Thème']").Change("2");

        Assert.Equal("Galet", chosenTheme?.Name);
        Assert.True(paletteReset);
        Assert.True(fontReset);
    }
}
