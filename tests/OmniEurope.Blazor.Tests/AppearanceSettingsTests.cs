using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

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
    public void Scale_rows_are_named_groups_with_a_named_slider_between_two_distinct_buttons_and_their_level()
    {
        int? textSize = null;
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.TextSizeLevel, 8)
            .Add(component => component.ControlSizeLevel, 3)
            .Add(component => component.TextSizeLevelChanged, value => textSize = value)
            .Add(component => component.ControlSizeLevelChanged, _ => { }));

        var groups = window.FindAll(".omni-appearance-settings--window .omni-appearance-settings__actions[role=group]");
        Assert.Equal(2, groups.Count);
        foreach (var group in groups)
        {
            var label = window.Find($"#{group.GetAttribute("aria-labelledby")}");
            Assert.Contains("omni-appearance-settings__label", label.ClassList);
            // R1-1: the slider is named by the row's label and speaks the percentage it draws.
            var slider = group.QuerySelector("input[type=range]")!;
            Assert.Equal(group.GetAttribute("aria-labelledby"), slider.GetAttribute("aria-labelledby"));
            Assert.Equal(["1", "10", "1"], [slider.GetAttribute("min")!, slider.GetAttribute("max")!, slider.GetAttribute("step")!]);
        }

        // The short labels sit under the "Texte" and "Disposition" headings; the level reads as the percentage it draws
        // (level 8 is 118.75 %, level 3 is 87.5 %).
        Assert.Equal("Taille", window.Find($"#{groups[0].GetAttribute("aria-labelledby")}").TextContent.Trim());
        Assert.Equal("Contrôles", window.Find($"#{groups[1].GetAttribute("aria-labelledby")}").TextContent.Trim());
        Assert.Equal(("8", "119 %", "119 %"), SliderOf(groups[0]));
        Assert.Equal(("3", "88 %", "88 %"), SliderOf(groups[1]));
        var names = window.FindAll(".omni-appearance-settings--window .omni-appearance-settings__actions[role=group] button[aria-label]").Select(button => button.GetAttribute("aria-label")).ToArray();
        Assert.Equal(
            ["Réduire la taille du texte", "Augmenter la taille du texte", "Défaut", "Réduire la taille des contrôles", "Augmenter la taille des contrôles", "Défaut"],
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

        choices[2].Click();
        Assert.Equal(OmniDensity.Spacious, density);
    }

    [Fact]
    public void Content_width_is_centred_or_full_in_the_window()
    {
        // Review R1-1: one choice between the host's centred column and the whole width.
        bool? fullWidth = null;
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.FullWidthChanged, value => fullWidth = value));

        var choices = window.FindAll("[role=radiogroup][aria-label='Largeur'] [role=radio]");
        Assert.Equal(["Centré", "Pleine largeur"], choices.Select(choice => choice.TextContent.Trim()));
        Assert.Equal("true", choices[0].GetAttribute("aria-checked"));

        choices[1].Click();
        Assert.True(fullWidth);
    }

    [Fact]
    public void Content_width_row_shows_only_once_bound()
    {
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.DensityChanged, _ => { }));

        Assert.Empty(window.FindAll("[aria-label='Largeur']"));
    }

    [Fact]
    public void Settings_pass_the_content_width_to_their_window()
    {
        bool? fullWidth = null;
        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.FullWidth, true)
            .Add(component => component.FullWidthChanged, value => fullWidth = value));

        settings.Find(".omni-appearance-settings__row--scale button").Click();
        var choices = settings.FindAll("[role=radiogroup][aria-label='Largeur'] [role=radio]");
        Assert.Equal("true", choices[1].GetAttribute("aria-checked"));

        choices[0].Click();
        Assert.False(fullWidth);
    }

    [Fact]
    public void Reset_all_and_restore_carry_the_content_width()
    {
        var fullWidth = true;
        var closes = 0;
        IRenderedComponent<OmniAppearanceWindow>? window = null;
        window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.FullWidth, fullWidth)
            .Add(component => component.FullWidthChanged, value => fullWidth = value)
            .Add(component => component.OpenChanged, _ => closes++));

        window.Find(".omni-appearance-window__reset").Click();
        Assert.False(fullWidth);
        window.Render(parameters => parameters.Add(component => component.FullWidth, fullWidth));

        window.Find(".omni-appearance-window__cancel").Click();
        Assert.True(fullWidth);
        Assert.Equal(1, closes);
    }

    [Fact]
    public void Text_size_window_row_steps_by_one_and_goes_back_to_the_default()
    {
        // Review R1-1 (owner decision of 2026-11-12): the row is one line, reduce, a slider with its percentage, enlarge, default.
        var levels = new List<int>();
        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.TextSizeLevel, 7)
            .Add(component => component.TextSizeLevelChanged, value => levels.Add(value))
            .Add(component => component.DensityChanged, _ => { }));

        settings.Find(".omni-appearance-settings__row--scale button").Click();
        var group = settings.Find(".omni-appearance-settings--window .omni-appearance-window__scale").ParentElement!;
        Assert.Equal(("7", "113 %", "113 %"), SliderOf(group));
        // The slider itself moves the level (one level per step), as the buttons do.
        group.QuerySelector("input[type=range]")!.Input("9");
        Assert.Equal([9], levels);
        levels.Clear();

        group.QuerySelector("button[aria-label='Réduire la taille du texte']")!.Click();
        group.QuerySelector("button[aria-label='Augmenter la taille du texte']")!.Click();
        group.QuerySelector("button[aria-label='Défaut']")!.Click();
        Assert.Equal([6, 8, 5], levels);
    }

    [Fact]
    public void The_size_steps_stop_at_levels_one_and_ten()
    {
        var levels = new List<int>();
        var lowest = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.TextSizeLevel, 1)
            .Add(component => component.TextSizeLevelChanged, value => levels.Add(value)));
        Assert.True(lowest.Find("button[aria-label='Réduire la taille du texte']").HasAttribute("disabled"));
        Assert.Equal(("1", "75 %", "75 %"), SliderOf(lowest.Find(".omni-appearance-settings--window")));
        lowest.Find("button[aria-label='Augmenter la taille du texte']").Click();

        var highest = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.TextSizeLevel, 10)
            .Add(component => component.TextSizeLevelChanged, value => levels.Add(value)));
        Assert.True(highest.Find("button[aria-label='Augmenter la taille du texte']").HasAttribute("disabled"));
        Assert.Equal(("10", "131 %", "131 %"), SliderOf(highest.Find(".omni-appearance-settings--window")));
        highest.Find("button[aria-label='Réduire la taille du texte']").Click();

        Assert.Equal([2, 9], levels);
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
    public async Task Window_keeps_a_palette_and_a_font_picked_while_the_host_still_writes_the_theme()
    {
        var events = new List<string>();
        var themeWritten = new TaskCompletionSource();
        var forest = OmniThemePalettes.All[1];
        var inter = OmniThemeFonts.All.Single(font => font.Name == "Inter");
        IRenderedComponent<OmniAppearanceWindow>? window = null;
        // The host writes the theme to an account: its callback completes later; it binds the palette and the
        // font back into the window as each one is picked.
        window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Preset, OmniThemePresets.All.Single(item => item.Name == "Papier"))
            .Add(component => component.Palette, OmniThemePalettes.All[0])
            .Add(component => component.Font, OmniThemeFonts.All.Single(font => font.Name == "JetBrains Mono"))
            .Add(component => component.PresetChanged, async value => { events.Add($"theme:{value?.Name}"); await themeWritten.Task; })
            .Add(component => component.PaletteChanged, value => { events.Add($"palette:{value?.Name ?? "null"}"); window!.Render(more => more.Add(component => component.Palette, value)); })
            .Add(component => component.FontChanged, value => { events.Add($"font:{value?.Name ?? "null"}"); window!.Render(more => more.Add(component => component.Font, value)); }));

        // The theme change is held until the host has written it: awaiting its handler, rather than waiting a
        // bounded time for a render, keeps the test independent of how busy the thread pool is.
        var themeChange = window.Find("select[aria-label='Thème']").ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "2" });
        await window.InvokeAsync(async () =>
        {
            await window.Instance.PaletteChanged.InvokeAsync(forest);
            await window.Instance.FontChanged.InvokeAsync(inter);
        });
        themeWritten.SetResult();
        await themeChange;

        Assert.Equal(["theme:Galet", $"palette:{forest.Name}", $"font:{inter.Name}"], events);
        Assert.Equal(forest, window.Instance.Palette);
        Assert.Equal(inter, window.Instance.Font);
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
        // A host that controls the settings (another callback bound) without the font never offers it.
        var unbound = Render<OmniAppearanceSettings>(parameters => parameters.Add(component => component.DensityChanged, _ => { }));
        unbound.Find(".omni-appearance-settings__row--scale button").Click();
        Assert.Empty(unbound.FindAll(".omni-appearance-window select[aria-label='Police']"));

        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.FontChanged, value => picked = value));
        settings.Find(".omni-appearance-settings__row--scale button").Click();

        // No inline row (review points 84 and 92): the window holds it; the drop-down posts the
        // option index.
        Assert.Empty(settings.FindAll(".omni-appearance-settings:not(.omni-appearance-settings--window) > .omni-appearance-settings__row select[aria-label='Police']"));
        var inWindow = settings.Find(".omni-appearance-window select[aria-label='Police']");
        var index = OmniThemeFonts.All.ToList().FindIndex(font => font.Name == "JetBrains Mono");
        inWindow.Change(index.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal("JetBrains Mono", picked?.Name);
    }

    private IRenderedComponent<OmniAppearanceWindow> RenderFullWindow() => Render<OmniAppearanceWindow>(parameters => parameters
        .Add(component => component.Open, true)
        .Add(component => component.Preset, OmniThemePresets.All.Single(theme => theme.Name == "Givre"))
        .Add(component => component.AppearanceChanged, _ => { })
        .Add(component => component.PresetChanged, _ => { })
        .Add(component => component.PaletteChanged, _ => { })
        .Add(component => component.BackdropChanged, _ => { })
        .Add(component => component.BackdropMotionChanged, _ => { })
        .Add(component => component.FontChanged, _ => { })
        .Add(component => component.TextSizeLevelChanged, _ => { })
        .Add(component => component.DensityChanged, _ => { })
        .Add(component => component.ControlSizeLevelChanged, _ => { })
        .Add(component => component.FullWidthChanged, _ => { }));

    private static string[] LabelsOf(AngleSharp.Dom.IElement section) =>
        [.. section.QuerySelectorAll(".omni-appearance-settings__row > .omni-appearance-settings__label").Select(label => label.TextContent.Trim())];

    /// <summary>The first slider under <paramref name="scope"/>: its value, the percentage written beside it and the one it speaks.</summary>
    private static (string Value, string Shown, string Spoken) SliderOf(AngleSharp.Dom.IElement scope)
    {
        var slider = scope.QuerySelector("input[type=range]")!;
        return (slider.GetAttribute("value")!, scope.QuerySelector(".omni-slider__value")!.TextContent.Trim(), slider.GetAttribute("aria-valuetext")!);
    }

    [Fact]
    public void Window_is_three_titled_sections_of_one_line_per_setting_in_the_order_they_are_read()
    {
        var window = RenderFullWindow();

        // Review R1-1, owner layout of 2026-11-12: Style (mode first, then theme, palette, background,
        // animation), Text (font, size), Layout (density, controls, width). The markup order is the order
        // on screen, so the keyboard follows what the eye reads.
        var sections = window.FindAll(".omni-appearance-settings--window > .omni-appearance-window__section");
        Assert.Equal(3, sections.Count);
        Assert.Equal(["Style", "Texte", "Disposition"], sections.Select(section => section.QuerySelector("h3.omni-appearance-window__heading")!.TextContent.Trim()));
        Assert.Equal(["Mode", "Thème", "Palette", "Fond", "Animation"], LabelsOf(sections[0]));
        Assert.Equal(["Police", "Taille"], LabelsOf(sections[1]));
        Assert.Equal(["Densité", "Contrôles", "Largeur"], LabelsOf(sections[2]));

        // Each section is a group named by its own heading (a real h3, not a bare span).
        foreach (var section in sections)
        {
            Assert.Equal("group", section.GetAttribute("role"));
            var heading = section.QuerySelector("h3")!;
            Assert.Equal(heading.Id, section.GetAttribute("aria-labelledby"));
            Assert.False(string.IsNullOrEmpty(heading.Id));
        }

        // One line per setting: a row holds one short label then one controls cell, nothing else.
        var rows = window.FindAll(".omni-appearance-settings--window .omni-appearance-settings__row");
        Assert.Equal(10, rows.Count);
        Assert.All(rows, row =>
        {
            Assert.Equal(2, row.Children.Length);
            Assert.Equal("omni-appearance-settings__label", row.Children[0].ClassName);
            Assert.Equal("omni-appearance-settings__actions", row.Children[1].ClassName);
        });
    }

    [Fact]
    public void Mode_has_the_first_line_to_itself_and_the_animation_a_line_of_its_own_under_the_background()
    {
        var window = RenderFullWindow();
        var rows = window.FindAll(".omni-appearance-settings--window .omni-appearance-settings__row");

        // Mode: the first line of the window, three icon buttons and no other control.
        var mode = rows[0];
        Assert.Equal("Mode", mode.QuerySelector(".omni-appearance-settings__label")!.TextContent.Trim());
        Assert.NotNull(mode.QuerySelector(".omni-appearance-window__modes"));
        Assert.Equal(3, mode.QuerySelectorAll("button").Length);
        Assert.Empty(mode.QuerySelectorAll("select, [role=switch]"));

        // Animation: right under Background, a row of its own holding the switch, labelled by the row's label.
        var background = rows.Single(row => row.QuerySelector(".omni-appearance-settings__label")!.TextContent.Trim() == "Fond");
        var animation = background.NextElementSibling!;
        Assert.Equal("Animation", animation.QuerySelector(".omni-appearance-settings__label")!.TextContent.Trim());
        var toggle = animation.QuerySelector("[role=switch]")!;
        Assert.Equal(animation.QuerySelector(".omni-appearance-settings__label")!.Id, toggle.GetAttribute("aria-labelledby"));
        foreach (var other in rows.Where(row => row != animation))
        {
            Assert.Empty(other.QuerySelectorAll("[role=switch]"));
        }
    }

    [Fact]
    public void Animation_row_shows_whenever_bound_and_is_enabled_only_over_a_backdrop_that_moves()
    {
        var plain = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.BackdropMotionChanged, _ => { }));
        // R1-4: over the plain page the row is still there, the switch disabled and off.
        var plainSwitch = Assert.Single(plain.FindAll("[role=switch]"));
        Assert.True(plainSwitch.HasAttribute("disabled"));
        Assert.Equal("false", plainSwitch.GetAttribute("aria-checked"));

        var unbound = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Preset, OmniThemePresets.All.Single(theme => theme.Name == "Givre"))
            .Add(component => component.PresetChanged, _ => { }));
        Assert.Empty(unbound.FindAll("[role=switch]"));

        var moving = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Preset, OmniThemePresets.All.Single(theme => theme.Name == "Givre"))
            .Add(component => component.BackdropMotionChanged, _ => { }));
        var movingSwitch = Assert.Single(moving.FindAll("[role=switch]"));
        Assert.False(movingSwitch.HasAttribute("disabled"));
        Assert.Equal(["Style"], moving.FindAll(".omni-appearance-window__heading").Select(heading => heading.TextContent.Trim()));
    }

    [Fact]
    public void A_section_shows_only_once_one_of_its_rows_is_bound()
    {
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.DensityChanged, _ => { }));

        Assert.Equal(["Disposition"], window.FindAll(".omni-appearance-window__heading").Select(heading => heading.TextContent.Trim()));
        Assert.Empty(window.FindAll(".omni-appearance-settings--window .omni-appearance-settings__row:not(:has(.omni-select-bar))"));
    }

    [Fact]
    public void Resets_are_compact_icon_buttons_named_and_titled_with_the_default_text()
    {
        var window = RenderFullWindow();

        // Theme, palette, background, font and the two scales: six, each icon only, named for assistive technologies and titled.
        var resets = window.FindAll(".omni-appearance-settings--window .omni-appearance-settings__reset");
        Assert.Equal(6, resets.Count);
        Assert.All(resets, reset =>
        {
            Assert.Equal("Défaut", reset.GetAttribute("aria-label"));
            Assert.Equal("Défaut", reset.GetAttribute("title"));
            Assert.NotNull(reset.QuerySelector("svg"));
            Assert.Equal(string.Empty, reset.TextContent.Trim());
            Assert.Contains("omni-button--secondary", reset.ClassList);
        });
    }

    [Fact]
    public void Window_is_one_narrow_column_of_a_label_column_and_a_control_column_on_desktop_and_phone()
    {
        // Only a browser measures the width; this pins the rules: a label column that gives up to 9rem, the
        // control column taking all that remains, rows and sections sharing both through subgrids, the
        // window never wider than the screen, and the same structure under 40rem (the label icons give
        // their width to the control).
        var grid = ShippedLookTests.Body(".omni-appearance-settings--window");
        Assert.Equal("fit-content(9rem) minmax(0, 1fr)", ShippedLookTests.Value(grid, "grid-template-columns"));
        Assert.Equal("subgrid", ShippedLookTests.Value(ShippedLookTests.Body(".omni-appearance-window__section"), "grid-template-columns"));
        Assert.Equal("subgrid", ShippedLookTests.Value(ShippedLookTests.Body(".omni-appearance-settings--window .omni-appearance-settings__row"), "grid-template-columns"));
        var box = ShippedLookTests.Body(".omni-appearance-window");
        Assert.Equal("36rem", ShippedLookTests.Value(box, "inline-size"));
        Assert.Equal("calc(100vw - 2rem)", ShippedLookTests.Value(box, "max-inline-size"));
        Assert.Equal("100%", ShippedLookTests.Value(ShippedLookTests.Body(".omni-appearance-settings--window .omni-select-bar"), "inline-size"));
        Assert.Equal("1 1 0", ShippedLookTests.Value(ShippedLookTests.Body(".omni-appearance-settings--window .omni-appearance-window__modes > .omni-button.omni-appearance-window__mode"), "flex"));
        Assert.Equal("none", ShippedLookTests.Value(ShippedLookTests.Rules().Last(rule => rule.Selector == ".omni-appearance-settings--window .omni-appearance-settings__label .omni-icon").Body, "display"));
    }

    [Fact]
    public void Mode_is_three_icon_buttons_on_its_line_and_restore_puts_it_back()
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

        // Three icon buttons joined in one group in the control cell of the Mode line, the chosen one read by its state.
        var group = window.Find(".omni-appearance-settings__row > .omni-appearance-settings__actions > .omni-appearance-window__modes");
        Assert.Equal("Mode", group.ParentElement!.PreviousElementSibling!.TextContent.Trim());
        var buttons = window.FindAll(".omni-appearance-window__modes > button[role=radio]");
        Assert.Equal(["Clair", "Sombre", "Système"], buttons.Select(button => button.GetAttribute("aria-label")).ToArray());
        Assert.All(buttons, button => Assert.NotNull(button.QuerySelector("svg")));
        Assert.Equal("true", buttons[0].GetAttribute("aria-checked"));

        // Joined: no radius between the buttons.
        Assert.Equal("0", ShippedLookTests.Value(ShippedLookTests.Body(".omni-appearance-window__modes > .omni-button.omni-appearance-window__mode"), "border-radius"));

        window.FindAll(".omni-appearance-window__modes > button[role=radio]")[1].Click();
        Assert.Equal(OmniAppearance.Dark, mode);
        Assert.Equal("true", window.FindAll(".omni-appearance-window__modes > button[role=radio]")[1].GetAttribute("aria-checked"));

        // Restore puts back the mode the window opened on, as every other setting.
        window.Find(".omni-appearance-window__cancel").Click();
        Assert.Equal(OmniAppearance.Light, mode);
    }

    [Fact]
    public void Mode_is_bound_on_its_own_and_is_fixed_under_a_dark_only_theme()
    {
        OmniAppearance? raised = null;
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Preset, (OmniThemePresets.All[0] with { DarkOnly = true }))
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
            // review point 90: the font is drawn too, never the one in force.
            Assert.NotEqual(fontBefore, (font ?? OmniThemePresets.DefaultFontFor(theme)).Name);
        }
    }

    [Fact]
    public void Random_draws_the_backdrop_too_among_those_the_new_theme_offers_and_touches_no_other_setting()
    {
        // R1-5 (owner decision of 2026-11-12): theme, palette, font and backdrop are drawn; mode, density, sizes and width are not.
        OmniThemePreset? theme = OmniThemePresets.All[0];
        OmniThemePalette? palette = null;
        OmniThemeFont? font = null;
        OmniThemeBackdrop? backdrop = null;
        var untouched = 0;
        // Written parameters keep the window controlled by the host (it would follow the state otherwise).
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.PresetChanged, _ => { }));
        var seen = new HashSet<string>();
        for (var draw = 0; draw < 80; draw++)
        {
            var inForce = AppearanceChoices.EffectiveBackdrop(theme, backdrop);
            var backdropEvents = 0;
            window.Render(parameters => parameters
                .Add(component => component.Preset, theme)
                .Add(component => component.Palette, palette)
                .Add(component => component.Font, font)
                .Add(component => component.Backdrop, backdrop)
                .Add(component => component.PresetChanged, value => theme = value)
                .Add(component => component.PaletteChanged, value => palette = value)
                .Add(component => component.FontChanged, value => font = value)
                .Add(component => component.BackdropChanged, value => { backdropEvents++; backdrop = value; })
                .Add(component => component.AppearanceChanged, _ => untouched++)
                .Add(component => component.DensityChanged, _ => untouched++)
                .Add(component => component.TextSizeLevelChanged, _ => untouched++)
                .Add(component => component.ControlSizeLevelChanged, _ => untouched++)
                .Add(component => component.FullWidthChanged, _ => untouched++));

            window.Find(".omni-appearance-settings__random").Click();

            Assert.Equal(1, backdropEvents);
            var drawn = AppearanceChoices.EffectiveBackdrop(theme, backdrop);
            Assert.Contains(drawn, OmniThemePresets.BackdropsFor(theme));
            Assert.NotSame(inForce, drawn);
            seen.Add(drawn.Name);
        }

        Assert.Equal(0, untouched);
        // The plain page is one of the offered backdrops (BackdropsFor holds it); the 80 draws pick far more than a couple of them.
        Assert.True(seen.Count > OmniThemeBackdrops.All.Count / 2, string.Join(", ", seen));
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
        // R1-7: Reset all stays available at the defaults too, since the host may hold extras of its own.
        Assert.False(window.Find(".omni-appearance-window__reset").HasAttribute("disabled"));
    }

    [Fact]
    public void Reset_all_under_the_default_theme_still_raises_every_bound_change_at_its_default_once()
    {
        // R1-7 (owner decision of 2026-11-12): a host that clears its own extras when the theme changes must hear
        // the theme change even when the theme is already the default, and every other bound setting too.
        var raised = new List<string>();
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.AppearanceChanged, value => raised.Add($"mode={value}"))
            .Add(component => component.PresetChanged, value => raised.Add($"theme={value?.Name ?? "default"}"))
            .Add(component => component.PaletteChanged, value => raised.Add($"palette={value?.Name ?? "default"}"))
            .Add(component => component.FontChanged, value => raised.Add($"font={value?.Name ?? "default"}"))
            .Add(component => component.BackdropChanged, value => raised.Add($"backdrop={value?.Name ?? "default"}"))
            .Add(component => component.BackdropMotionChanged, value => raised.Add($"motion={value}"))
            .Add(component => component.TextSizeLevelChanged, value => raised.Add($"text={value}"))
            .Add(component => component.DensityChanged, value => raised.Add($"density={value}"))
            .Add(component => component.ControlSizeLevelChanged, value => raised.Add($"controls={value}"))
            .Add(component => component.FullWidthChanged, value => raised.Add($"width={value}")));

        window.Find(".omni-appearance-window__reset").Click();

        Assert.Equal(
            ["mode=System", "theme=default", "palette=default", "font=default", "backdrop=default", "motion=True", "text=5", "density=Comfortable", "controls=5", "width=False"],
            raised);
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
        // Owner decision of 2026-10-02 (review point 88): closing is not restoring.
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
        // Review R1-1, owner decision of 2026-11-12: cancelling is not destructive, so Cancel is the grey
        // Secondary, not the red Danger, and it comes before Apply (the validated footer: random, reset all,
        // Cancel, Apply; this replaces the order of 2026-10-02, review point 87).
        Assert.Contains("omni-button--secondary", restore.ClassList);
        Assert.DoesNotContain("omni-button--danger", restore.ClassList);
        Assert.Contains("omni-appearance-window__apply", restore.NextElementSibling!.ClassList);
        Assert.Equal(
            ["omni-appearance-settings__random", "omni-appearance-window__reset", "omni-appearance-window__cancel", "omni-appearance-window__apply"],
            window.FindAll(".omni-appearance-window__footer button").Select(button => button.ClassList.Last(name => name.StartsWith("omni-appearance-", StringComparison.Ordinal))));
        apply.Click();

        Assert.Equal(changes, host.Changes);
        Assert.Equal((5, OmniDensity.Comfortable), (host.TextSize, host.Density));
        Assert.Equal([false], host.Closes);
    }

    [Fact]
    public void The_window_never_widens_whatever_the_theme_its_controls_row_wraps_and_its_select_gives_way()
    {
        // Only a browser measures the overflow (the probe sweeps every theme); this pins the rules that
        // prevent it: the controls row wraps instead of running past its track, the select shrinks first
        // and the buttons keep their width, and a row can shrink to its track.
        const string window = ".omni-appearance-settings--window ";
        var actions = ShippedLookTests.Body(window + ".omni-appearance-settings__actions");
        Assert.Equal("wrap", ShippedLookTests.Value(actions, "flex-wrap"));
        Assert.DoesNotContain("nowrap", actions, StringComparison.Ordinal);
        Assert.Equal("0", ShippedLookTests.Value(actions, "min-inline-size"));
        Assert.Equal("flex", ShippedLookTests.Value(ShippedLookTests.Body(window + ".omni-appearance-settings__actions:has(.omni-drop-down)"), "display"));

        var select = ShippedLookTests.Body(window + ".omni-appearance-settings__actions > .omni-drop-down");
        Assert.Equal("1 1 8rem", ShippedLookTests.Value(select, "flex"));
        Assert.Equal("0", ShippedLookTests.Value(select, "min-inline-size"));
        Assert.Equal("none", ShippedLookTests.Value(ShippedLookTests.Body(window + ".omni-appearance-settings__actions > .omni-button"), "flex"));
        Assert.Equal("0", ShippedLookTests.Value(ShippedLookTests.Body(window + ".omni-appearance-settings__row"), "min-inline-size"));

        // The footer wraps its two groups, which share the line, and a button wraps its text only when its whole row is too narrow.
        Assert.Equal("wrap", ShippedLookTests.Value(ShippedLookTests.Body(".omni-appearance-window__footer"), "flex-wrap"));
        var footerButton = ShippedLookTests.Body(".omni-appearance-window__footer .omni-button");
        Assert.DoesNotContain("nowrap", footerButton, StringComparison.Ordinal);
        Assert.Equal("1 1 auto", ShippedLookTests.Value(footerButton, "flex"));
        Assert.Equal("100%", ShippedLookTests.Value(footerButton, "max-inline-size"));
        Assert.Equal("1 1 auto", ShippedLookTests.Value(ShippedLookTests.Body(".omni-appearance-window__tools, .omni-appearance-window__decision"), "flex"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    public void The_percentage_of_a_scale_row_is_the_scale_the_root_draws_for_that_level(int level)
    {
        // The row says "119 %" for level 8: the same factor data-oe-text-size and data-oe-control-size apply.
        var percent = OmniAppearanceWindow.ScalePercent(level);
        var text = ShippedLookTests.Value(ShippedLookTests.Body($"html[data-oe-text-size=\"{level}\"]"), "font-size");
        var control = ShippedLookTests.Value(ShippedLookTests.Body($"html[data-oe-control-size=\"{level}\"]"), "--omni-control-scale");

        Assert.Equal(percent, double.Parse(text.TrimEnd('%'), System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(percent / 100, double.Parse(control, System.Globalization.CultureInfo.InvariantCulture));
    }
}