using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class AppearanceSettingsTests : OmniBunitContext
{
    [Fact]
    public void Theme_picker_has_one_default_choice()
    {
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.PresetChanged, _ => { }));
        var options = window.Find("select[aria-label='Thème']").QuerySelectorAll("option");

        Assert.Equal(OmniThemePresets.All.Count, options.Length);
        Assert.Single(options, option => option.TextContent == "Essentiel (défaut)");
        Assert.DoesNotContain(options, option => option.TextContent.Contains("paquet", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("Ardoise", "Océan")]
    [InlineData("Galet", "Forêt")]
    public void Theme_palette_is_named_in_the_selector(string themeName, string paletteName)
    {
        var theme = OmniThemePresets.All.Single(item => item.Name == themeName);
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Preset, theme)
            .Add(component => component.PaletteChanged, _ => { }));

        Assert.Contains($"{paletteName} (défaut)", window.Find("select[aria-label='Palette']").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Settings_offer_no_theme_or_palette_row_of_their_own()
    {
        // The window owns the theme and the palette: the settings list them nowhere, until it opens.
        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.PresetChanged, _ => { })
            .Add(component => component.PaletteChanged, _ => { }));

        Assert.Empty(settings.FindAll("select[aria-label='Thème'], select[aria-label='Palette']"));
        settings.Find(".omni-appearance-settings__row--scale button").Click();
        Assert.Single(settings.FindAll("select[aria-label='Thème']"));
        Assert.Single(settings.FindAll("select[aria-label='Palette']"));
    }

    [Fact]
    public void Mode_is_a_radio_group_whose_choice_reads_by_its_state()
    {
        OmniAppearance? picked = null;
        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.Dark)
            .Add(component => component.AppearanceChanged, value => picked = value));

        var group = settings.Find("[role=radiogroup][aria-label='Mode']");
        var modes = group.QuerySelectorAll("[role=radio]");
        Assert.Equal(["Clair", "Sombre", "Système"], modes.Select(mode => mode.TextContent.Trim()));
        Assert.Equal(["false", "true", "false"], modes.Select(mode => mode.GetAttribute("aria-checked")));

        modes[0].Click();
        Assert.Equal(OmniAppearance.Light, picked);
    }

    [Fact]
    public void Scale_rows_are_named_groups_with_distinct_buttons_and_their_level()
    {
        int? textSize = null;
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.TextSizeLevel, 8)
            .Add(component => component.ControlSizeLevel, 3)
            .Add(component => component.TextSizeLevelChanged, value => textSize = value)
            .Add(component => component.ControlSizeLevelChanged, _ => { }));

        var groups = window.FindAll(".omni-appearance-settings--window [role=group]");
        Assert.Equal(2, groups.Count);
        foreach (var group in groups)
        {
            var label = window.Find($"#{group.GetAttribute("aria-labelledby")}");
            Assert.Contains("omni-appearance-settings__label", label.ClassList);
            Assert.NotNull(group.QuerySelector($"#{group.GetAttribute("aria-describedby")}.omni-badge"));
        }

        Assert.Equal("Taille du texte", window.Find($"#{groups[0].GetAttribute("aria-labelledby")}").TextContent.Trim());
        Assert.Equal("8/10", window.Find($"#{groups[0].GetAttribute("aria-describedby")}").TextContent.Trim());
        var names = window.FindAll(".omni-appearance-settings--window button[aria-label]").Select(button => button.GetAttribute("aria-label")).ToArray();
        Assert.Equal(
            ["Réduire la taille du texte", "Augmenter la taille du texte", "Réduire la taille des contrôles", "Augmenter la taille des contrôles"],
            names);

        groups[0].QuerySelector("button[aria-label='Augmenter la taille du texte']")!.Click();
        Assert.Equal(9, textSize);
        groups[0].QuerySelectorAll("button").Last().Click();
        Assert.Equal(5, textSize);
    }

    [Fact]
    public void Density_is_a_choice_of_three_in_the_window()
    {
        OmniDensity? density = null;
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Density, OmniDensity.Comfortable)
            .Add(component => component.DensityChanged, value => density = value));

        var choices = window.FindAll("[role=radiogroup][aria-label='Densité'] [role=radio]");
        Assert.Equal(["Compacte", "Confortable", "Aérée"], choices.Select(choice => choice.TextContent.Trim()));
        Assert.Equal("true", choices[1].GetAttribute("aria-checked"));
        Assert.Empty(window.FindAll("input[type=range]"));

        choices[2].Click();
        Assert.Equal(OmniDensity.Spacious, density);
    }

    [Fact]
    public void Text_size_window_row_has_a_slider()
    {
        int? textSize = null;
        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.TextSizeLevelChanged, value => textSize = value)
            .Add(component => component.DensityChanged, _ => { }));

        settings.Find(".omni-appearance-settings__row--scale button").Click();
        var slider = settings.Find(".omni-appearance-settings--window input[type=range]");
        Assert.Equal(("1", "10"), (slider.GetAttribute("min"), slider.GetAttribute("max")));
        Assert.Equal("Taille du texte", slider.GetAttribute("aria-label"));

        slider.Input("8");
        Assert.Equal(8, textSize);
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

        // The font and the theme are picked in the window. The drop-down posts the option index: Galet
        // is the third theme.
        settings.Find(".omni-appearance-settings__row--scale button").Click();
        var options = settings.Find(".omni-appearance-window select[aria-label='Police']").QuerySelectorAll("option");
        Assert.Equal(10, options.Length);
        Assert.Single(options, option => option.TextContent == "Source Serif (défaut)");
        Assert.Equal("Source Serif", OmniThemePresets.DefaultFontFor(theme).Name);
        settings.Find("select[aria-label='Thème']").Change("2");

        Assert.Equal("Galet", chosenTheme?.Name);
        Assert.True(paletteReset);
        Assert.True(fontReset);
    }

    [Fact]
    public void Window_alone_resets_palette_and_font_on_a_new_theme_like_the_settings()
    {
        var events = new List<string>();
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Preset, OmniThemePresets.All.Single(item => item.Name == "Papier"))
            .Add(component => component.Palette, OmniThemePalettes.All[0])
            .Add(component => component.Font, OmniThemeFonts.All.Single(font => font.Name == "JetBrains Mono"))
            .Add(component => component.PresetChanged, value => events.Add($"theme:{value?.Name}"))
            .Add(component => component.PaletteChanged, value => events.Add($"palette:{value?.Name ?? "null"}"))
            .Add(component => component.FontChanged, value => events.Add($"font:{value?.Name ?? "null"}")));

        Assert.Single(window.FindAll("select[aria-label='Police']"));
        window.Find("select[aria-label='Thème']").Change("2");

        Assert.Equal(["theme:Galet", "palette:null", "font:null"], events);
    }

    [Fact]
    public void Edit_is_the_main_action_of_its_row()
    {
        var settings = Render<OmniAppearanceSettings>();

        var edit = settings.Find(".omni-appearance-settings__row--scale button");
        Assert.Contains("omni-button--primary", edit.ClassList);
        Assert.DoesNotContain("omni-button--success", edit.ClassList);
    }

    [Fact]
    public void Font_is_offered_in_the_window_only_once_the_host_binds_it()
    {
        OmniThemeFont? picked = null;
        var unbound = Render<OmniAppearanceSettings>();
        unbound.Find(".omni-appearance-settings__row--scale button").Click();
        Assert.Empty(unbound.FindAll(".omni-appearance-window select[aria-label='Police']"));

        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.FontChanged, value => picked = value));
        settings.Find(".omni-appearance-settings__row--scale button").Click();

        // No inline row (Atlas review points 84 and 92): the window holds it; the drop-down posts the
        // option index.
        Assert.Empty(settings.FindAll(".omni-appearance-settings:not(.omni-appearance-settings--window) > .omni-appearance-settings__row select[aria-label='Police']"));
        var inWindow = settings.Find(".omni-appearance-window select[aria-label='Police']");
        var index = OmniThemeFonts.All.ToList().FindIndex(font => font.Name == "JetBrains Mono");
        inWindow.Change(index.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal("JetBrains Mono", picked?.Name);
    }

    [Fact]
    public void Window_rows_go_two_by_two_in_the_order_they_are_read()
    {
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Preset, OmniThemePresets.All.Single(theme => theme.Name == "Givre"))
            .Add(component => component.PresetChanged, _ => { })
            .Add(component => component.PaletteChanged, _ => { })
            .Add(component => component.FontChanged, _ => { })
            .Add(component => component.BackdropMotionChanged, _ => { })
            .Add(component => component.TextSizeLevelChanged, _ => { })
            .Add(component => component.DensityChanged, _ => { })
            .Add(component => component.ControlSizeLevelChanged, _ => { }));

        // Owner order of 2026-10-02 (Atlas review point 85): theme beside font, palette beside density,
        // then the two sizes. The order of the markup is the order on screen, so the keyboard follows
        // what the eye reads.
        var names = window.FindAll(".omni-appearance-settings--window > .omni-appearance-settings__row > .omni-appearance-settings__label")
            .Select(label => label.TextContent.Trim());
        Assert.Equal(["Thème", "Police", "Palette", "Densité", "Taille du texte", "Taille des contrôles"], names);
        // The motion of a moving theme sits at the end of its title line (point 86), right after the
        // label and before the list, not in a row of its own.
        var motion = window.Find(".omni-appearance-settings__row:first-child > .omni-appearance-settings__label + .omni-appearance-settings__motion");
        Assert.Equal("Fond animé", motion.TextContent.Trim());
        Assert.Equal("omni-appearance-settings__actions", motion.NextElementSibling!.ClassName);
        Assert.Equal("0 0 auto", ShippedLookTests.Value(ShippedLookTests.Body(".omni-appearance-settings__label + .omni-appearance-settings__motion"), "flex"));

        // Two columns of at least 19rem in a window of 46rem: two fit, a third never does, and one
        // remains when the screen narrows the window.
        Assert.Equal(
            "repeat(auto-fit, minmax(min(100%, 19rem), 1fr))",
            ShippedLookTests.Value(ShippedLookTests.Body(".omni-appearance-settings--window"), "grid-template-columns"));
        var box = ShippedLookTests.Body(".omni-appearance-window");
        Assert.Equal("46rem", ShippedLookTests.Value(box, "inline-size"));
        Assert.Equal("calc(100vw - 2rem)", ShippedLookTests.Value(box, "max-inline-size"));
    }

    [Fact]
    public void Mode_joins_the_palette_row_as_three_icon_buttons_and_restore_puts_it_back()
    {
        var mode = OmniAppearance.Light;
        IRenderedComponent<OmniAppearanceWindow> window = null!;
        window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Appearance, mode)
            .Add(component => component.AppearanceChanged, value =>
            {
                mode = value;
                window.Render(update => update.Add(component => component.Appearance, value));
            })
            .Add(component => component.PaletteChanged, _ => { })
            .Add(component => component.OpenChanged, _ => { }));

        // Owner request of 2026-10-02 (Atlas review point 92): in the palette tile, three icon buttons
        // joined in one group on the title line, right after the label and before the list, which keeps
        // its width; the chosen one reads by its state.
        var modesGroup = window.Find(".omni-appearance-settings__row > .omni-appearance-settings__label + .omni-appearance-window__modes");
        Assert.Equal("Palette", modesGroup.PreviousElementSibling!.TextContent.Trim());
        Assert.Equal("omni-appearance-settings__actions", modesGroup.NextElementSibling!.ClassName);
        Assert.NotNull(modesGroup.NextElementSibling.QuerySelector("select[aria-label='Palette']"));
        var buttons = window.FindAll(".omni-appearance-window__modes > button[role=radio]");
        Assert.Equal(["Clair", "Sombre", "Système"], buttons.Select(button => button.GetAttribute("aria-label")).ToArray());
        Assert.All(buttons, button => Assert.NotNull(button.QuerySelector("svg")));
        Assert.Equal("true", buttons[0].GetAttribute("aria-checked"));

        // Joined: no radius between the buttons; pushed to the end of the title line.
        Assert.Equal("0", ShippedLookTests.Value(ShippedLookTests.Body(".omni-app-menu__modes > .omni-button.omni-app-menu__mode, .omni-appearance-window__modes > .omni-button.omni-appearance-window__mode"), "border-radius"));
        Assert.Equal("auto", ShippedLookTests.Value(ShippedLookTests.Body(".omni-appearance-settings__label + .omni-appearance-window__modes"), "margin-inline-start"));

        window.FindAll(".omni-appearance-window__modes > button[role=radio]")[1].Click();
        Assert.Equal(OmniAppearance.Dark, mode);
        Assert.Equal("true", window.FindAll(".omni-appearance-window__modes > button[role=radio]")[1].GetAttribute("aria-checked"));

        // Restore puts back the mode the window opened on, as every other setting.
        window.Find(".omni-appearance-window__cancel").Click();
        Assert.Equal(OmniAppearance.Light, mode);
    }

    [Fact]
    public void Mode_keeps_a_row_of_its_own_without_a_palette_and_is_fixed_under_a_dark_only_theme()
    {
        OmniAppearance? raised = null;
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Preset, OmniThemePresets.All.First(theme => theme.DarkOnly))
            .Add(component => component.Appearance, OmniAppearance.Light)
            .Add(component => component.AppearanceChanged, value => raised = value));

        var row = window.Find(".omni-appearance-settings__row:has(.omni-appearance-window__modes)");
        Assert.Equal("Mode", row.QuerySelector(".omni-appearance-settings__label")!.TextContent.Trim());
        var buttons = window.FindAll(".omni-appearance-window__modes > button[role=radio]");
        Assert.All(buttons, button => Assert.True(button.HasAttribute("disabled")));
        Assert.Equal("true", buttons[1].GetAttribute("aria-checked"));
        Assert.Null(raised);

        // Unbound, the window offers no mode at all.
        var unbound = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.PaletteChanged, _ => { }));
        Assert.Empty(unbound.FindAll(".omni-appearance-window__modes"));
    }

    [Fact]
    public void Settings_open_a_window_that_carries_the_mode()
    {
        OmniAppearance? raised = null;
        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.PaletteChanged, _ => { })
            .Add(component => component.AppearanceChanged, value => raised = value));
        settings.Find(".omni-appearance-settings__row--scale button").Click();

        settings.FindAll(".omni-appearance-window__modes > button[role=radio]")[1].Click();
        Assert.Equal(OmniAppearance.Dark, raised);
    }

    [Fact]
    public void Random_draws_another_theme_another_palette_and_another_font()
    {
        var theme = OmniThemePresets.All.Single(item => item.Name == "Papier");
        OmniThemePalette? palette = OmniThemePalettes.All.Single(item => item.Name == "Mono");
        OmniThemeFont? font = OmniThemeFonts.All.Single(item => item.Name == "JetBrains Mono");
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Preset, theme)
            .Add(component => component.Palette, palette)
            .Add(component => component.Font, font));

        for (var draw = 0; draw < 60; draw++)
        {
            var themeBefore = theme;
            var paletteBefore = (palette ?? OmniThemePresets.DefaultPaletteFor(theme)).Name;
            var fontBefore = (font ?? OmniThemePresets.DefaultFontFor(theme)).Name;
            var themeEvents = 0;
            var paletteEvents = 0;
            var fontEvents = 0;
            window.Render(parameters => parameters
                .Add(component => component.Preset, theme)
                .Add(component => component.Palette, palette)
                .Add(component => component.Font, font)
                .Add(component => component.PresetChanged, value => { themeEvents++; theme = value ?? OmniThemePresets.All[0]; })
                .Add(component => component.PaletteChanged, value => { paletteEvents++; palette = value; })
                .Add(component => component.FontChanged, value => { fontEvents++; font = value; }));

            var random = window.Find(".omni-appearance-settings__random");
            Assert.Equal("Aléatoire", random.TextContent.Trim());
            random.Click();

            Assert.Equal((1, 1, 1), (themeEvents, paletteEvents, fontEvents));
            Assert.Contains(theme, OmniThemePresets.All);
            Assert.NotSame(themeBefore, theme);
            Assert.NotEqual(paletteBefore, (palette ?? OmniThemePresets.DefaultPaletteFor(theme)).Name);
            // Atlas review point 90: the font is drawn too, never the one in force.
            Assert.NotEqual(fontBefore, (font ?? OmniThemePresets.DefaultFontFor(theme)).Name);
        }
    }

    [Fact]
    public void Random_draws_a_theme_alone_when_the_palette_is_not_bound()
    {
        OmniThemePreset? drawn = OmniThemePresets.All[0];
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.PresetChanged, value => drawn = value));

        window.Find(".omni-appearance-settings__random").Click();

        // The first theme is the one in force: the draw is any other, never null.
        Assert.NotNull(drawn);
        Assert.NotSame(OmniThemePresets.All[0], drawn);
    }

    [Fact]
    public void Window_open_state_is_reported_and_can_be_set_by_the_host()
    {
        var reported = new List<bool>();
        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.WindowOpenChanged, value => reported.Add(value)));

        Assert.Empty(settings.FindAll(".omni-appearance-window"));
        settings.Find(".omni-appearance-settings__row--scale button").Click();
        Assert.Equal([true], reported);
        Assert.Single(settings.FindAll(".omni-appearance-window"));

        var opened = Render<OmniAppearanceSettings>(parameters => parameters.Add(component => component.WindowOpen, true));
        Assert.Single(opened.FindAll(".omni-appearance-window"));
    }

    [Fact]
    public void Id_and_additional_attributes_reach_the_root()
    {
        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.Id, "reglages")
            .AddUnmatched("data-zone", "entete")
            .AddUnmatched("aria-label", "Apparence"));

        var root = settings.Find(".omni-appearance-settings");
        Assert.Equal("reglages", root.GetAttribute("id"));
        Assert.Equal("entete", root.GetAttribute("data-zone"));
        Assert.Equal("Apparence", root.GetAttribute("aria-label"));
    }

    /// <summary>A host that applies every change it receives, as a real one does, and records the closes.</summary>
    private sealed class LookHost
    {
        public OmniThemePreset? Preset;
        public int TextSize = 8;
        public OmniDensity Density = OmniDensity.Spacious;
        public readonly List<bool> Closes = [];
        public int Changes;
    }

    private IRenderedComponent<OmniAppearanceWindow> RenderLookWindow(LookHost host)
    {
        IRenderedComponent<OmniAppearanceWindow>? window = null;
        void Bind(ComponentParameterCollectionBuilder<OmniAppearanceWindow> parameters) => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Preset, host.Preset)
            .Add(component => component.TextSizeLevel, host.TextSize)
            .Add(component => component.Density, host.Density)
            .Add(component => component.PresetChanged, value => { host.Changes++; host.Preset = value; })
            .Add(component => component.TextSizeLevelChanged, value => { host.Changes++; host.TextSize = value; })
            .Add(component => component.DensityChanged, value => { host.Changes++; host.Density = value; })
            .Add(component => component.OpenChanged, value => host.Closes.Add(value));
        window = Render<OmniAppearanceWindow>(Bind);
        return window;
    }

    private static void Reapply(IRenderedComponent<OmniAppearanceWindow> window, LookHost host) => window.Render(parameters => parameters
        .Add(component => component.Preset, host.Preset)
        .Add(component => component.TextSizeLevel, host.TextSize)
        .Add(component => component.Density, host.Density));

    [Fact]
    public void Reset_all_puts_every_bound_setting_back_to_its_default_and_keeps_the_window_open()
    {
        var host = new LookHost { Preset = OmniThemePresets.All[3] };
        var window = RenderLookWindow(host);

        window.Find(".omni-appearance-window__reset").Click();

        Assert.Equal((null, 5, OmniDensity.Comfortable), (host.Preset, host.TextSize, host.Density));
        Assert.Empty(host.Closes);
        Reapply(window, host);
        Assert.True(window.Find(".omni-appearance-window__reset").HasAttribute("disabled"));
    }

    [Fact]
    public void Restore_puts_back_the_look_the_window_opened_on()
    {
        var opened = OmniThemePresets.All[2];
        var host = new LookHost { Preset = opened };
        var window = RenderLookWindow(host);
        window.Find(".omni-appearance-window__reset").Click();
        Reapply(window, host);

        window.Find(".omni-appearance-window__cancel").Click();

        Assert.Equal((opened, 8, OmniDensity.Spacious), (host.Preset, host.TextSize, host.Density));
        Assert.Equal([false], host.Closes);
    }

    [Fact]
    public void The_close_button_only_closes_and_keeps_the_look_tried()
    {
        // Owner decision of 2026-10-02 (Atlas review point 88): closing is not restoring.
        var host = new LookHost { Preset = OmniThemePresets.All[2] };
        var window = RenderLookWindow(host);
        window.Find(".omni-appearance-window__reset").Click();
        Reapply(window, host);
        var changes = host.Changes;

        window.Find(".omni-dialog__close").Click();

        Assert.Equal(changes, host.Changes);
        Assert.Equal((null, 5, OmniDensity.Comfortable), (host.Preset, host.TextSize, host.Density));
        Assert.Equal([false], host.Closes);
    }

    [Fact]
    public void Apply_keeps_the_look_tried_and_only_closes()
    {
        var host = new LookHost();
        var window = RenderLookWindow(host);
        window.Find(".omni-appearance-window__reset").Click();
        Reapply(window, host);
        var changes = host.Changes;

        var apply = window.Find(".omni-appearance-window__apply");
        Assert.Equal("Valider", apply.TextContent.Trim());
        Assert.Contains("omni-button--primary", apply.ClassList);
        var restore = window.Find(".omni-appearance-window__cancel");
        Assert.Equal("Restaurer", restore.TextContent.Trim());
        Assert.Contains("omni-button--danger", restore.ClassList);
        // Owner decision of 2026-10-02 (Atlas review point 87): Apply comes before Restore.
        Assert.Contains("omni-appearance-window__cancel", apply.NextElementSibling!.ClassList);
        apply.Click();

        Assert.Equal(changes, host.Changes);
        Assert.Equal((5, OmniDensity.Comfortable), (host.TextSize, host.Density));
        Assert.Equal([false], host.Closes);
    }
}
