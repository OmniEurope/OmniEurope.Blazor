using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class AppMenuTests : OmniBunitContext
{
    [Fact]
    public void Without_an_account_the_menu_offers_the_mode_and_the_theme_and_nothing_that_does_nothing()
    {
        var menu = Render<OmniAppMenu>(parameters => parameters
            .Add(component => component.AppearanceChanged, _ => { })
            .Add(component => component.OnThemeOpen, () => { }));

        var trigger = menu.Find(".omni-app-menu__trigger");
        Assert.Equal("Menu de l’application", trigger.GetAttribute("aria-label"));
        Assert.Empty(menu.FindAll(".omni-app-menu__trigger-name"));
        // No name: the trigger is its icon alone, square like every icon-only button.
        Assert.Contains("omni-button--icon", trigger.ClassList);
        Assert.Single(menu.FindAll(".omni-app-menu__trigger > .omni-button__content > .omni-icon"));
        trigger.Click();

        Assert.Empty(menu.FindAll(".omni-app-menu__identity"));
        Assert.Empty(menu.FindAll(".omni-app-menu__language"));
        Assert.Empty(menu.FindAll(".omni-app-menu__settings"));
        Assert.Empty(menu.FindAll(".omni-app-menu__footer"));
        Assert.Single(menu.FindAll("div.omni-app-menu__mode"));
        // The only radio group is the mode: three buttons, nothing else that picks.
        Assert.Single(menu.FindAll("[role=radiogroup]"));
        Assert.Equal(3, menu.FindAll("[role=radio]").Count);
        Assert.Equal("Thème", menu.Find(".omni-app-menu__theme").TextContent.Trim());
    }

    [Fact]
    public void The_panel_takes_the_width_of_its_widest_row_instead_of_a_fixed_width()
    {
        // The width is the stylesheet's, so only a browser measures it; this pins the rule that sets it
        // (recette R-3): content-sized, with a floor, and no fixed width back on the panel.
        var body = ShippedLookTests.Body(".omni-app-menu .omni-popover__panel");
        Assert.Equal("max-content", ShippedLookTests.Value(body, "inline-size"));
        Assert.Equal("10rem", ShippedLookTests.Value(body, "min-inline-size"));
        Assert.DoesNotContain("20rem", body, StringComparison.Ordinal);

        // The panel keeps the popover's own cap (24rem or the viewport), which a wide host row cannot lift.
        var popover = ShippedLookTests.Body(".omni-popover__panel");
        Assert.Contains("max-inline-size: min(24rem, calc(100vw - 2 * var(--omni-space-md)))", popover, StringComparison.Ordinal);

        // The markup the rule sizes: the panel is still the popover's, holding one card of rows.
        var menu = Render<OmniAppMenu>(parameters => parameters
            .Add(component => component.AppearanceChanged, _ => { }));
        menu.Find(".omni-app-menu__trigger").Click();
        Assert.Single(menu.FindAll(".omni-app-menu .omni-popover__panel > .omni-app-menu__card"));
    }

    [Fact]
    public void On_a_phone_the_trigger_drops_the_name_which_stays_in_the_menu()
    {
        // The width query is the stylesheet's (the browser applies it; recette R-034): the name
        // overflowed the app bar at 375 px. The trigger keeps its label, the open menu names the user.
        Assert.Matches(@"@media \(max-width: 39\.99rem\) \{ \.omni-app-menu__trigger-name \{ display: none; \} \}", StylesheetSource.Read());

        var menu = Render<OmniAppMenu>(parameters => parameters
            .Add(component => component.UserName, "admin")
            .Add(component => component.AppearanceChanged, _ => { }));
        Assert.Equal("Menu de l’application", menu.Find(".omni-app-menu__trigger").GetAttribute("aria-label"));
        // With a name, the trigger holds the user icon, the name and the chevron, and is not square.
        Assert.DoesNotContain("omni-button--icon", menu.Find(".omni-app-menu__trigger").ClassList);
        Assert.Equal(2, menu.FindAll(".omni-app-menu__trigger .omni-icon").Count);
        menu.Find(".omni-app-menu__trigger").Click();
        Assert.Equal("admin", menu.Find(".omni-app-menu__identity .omni-app-menu__label").TextContent);
    }

    [Fact]
    public void Every_row_shows_once_the_host_gives_what_it_needs_in_the_order_of_the_standard()
    {
        var menu = Render<OmniAppMenu>(parameters => parameters
            .Add(component => component.UserName, "admin")
            .Add(component => component.Role, "Admin")
            .Add(component => component.Languages, [new OmniAppMenuLanguage("fr", "Français", "flags/fr.svg"), new OmniAppMenuLanguage("en", "English")])
            .Add(component => component.Language, "fr")
            .Add(component => component.ShowFlags, true)
            .Add(component => component.AppearanceChanged, _ => { })
            .Add(component => component.OnThemeOpen, () => { })
            .Add(component => component.OnSettingsOpen, () => { })
            .Add(component => component.Version, "Boutique v0.4.0")
            .Add(component => component.OnSignOut, () => { })
            .AddChildContent("<div class=\"host-row\">Organisation</div>"));

        Assert.Equal("admin", menu.Find(".omni-app-menu__trigger-name").TextContent);
        menu.Find(".omni-app-menu__trigger").Click();

        var card = menu.Find(".omni-app-menu__card");
        var order = card.Children.Select(row => row.ClassList.FirstOrDefault(name => name.StartsWith("omni-app-menu__", StringComparison.Ordinal) && name != "omni-app-menu__row" && name != "omni-app-menu__item") ?? row.ClassName ?? string.Empty).ToArray();
        Assert.Equal("Admin", menu.Find(".omni-app-menu__identity .omni-badge").TextContent.Trim());
        Assert.Equal("Organisation", menu.Find(".omni-app-menu__extra .host-row").TextContent);
        Assert.Equal("flags/fr.svg", menu.Find(".omni-app-menu__flag").GetAttribute("src"));
        Assert.Equal("Boutique v0.4.0", menu.Find(".omni-app-menu__version").TextContent);
        Assert.Equal("Déconnexion", menu.Find(".omni-app-menu__sign-out").TextContent.Trim());
        Assert.Equal("Paramètres", menu.Find(".omni-app-menu__settings").TextContent.Trim());
        Assert.Equal("omni-app-menu__identity", order[0]);
        Assert.Equal("omni-app-menu__extra", order[1]);
        Assert.Equal("omni-app-menu__footer", order[^1]);
    }

    [Fact]
    public void The_language_row_needs_two_languages_and_hides_the_flag_unless_asked()
    {
        var single = Render<OmniAppMenu>(parameters => parameters
            .Add(component => component.Languages, [new OmniAppMenuLanguage("fr", "Français", "flags/fr.svg")])
            .Add(component => component.Language, "fr")
            .Add(component => component.AppearanceChanged, _ => { })
            .Add(component => component.OnThemeOpen, () => { }));
        single.Find(".omni-app-menu__trigger").Click();
        Assert.Empty(single.FindAll(".omni-app-menu__language"));

        string? picked = null;
        var two = Render<OmniAppMenu>(parameters => parameters
            .Add(component => component.Languages, [new OmniAppMenuLanguage("fr", "Français", "flags/fr.svg"), new OmniAppMenuLanguage("en", "English")])
            .Add(component => component.Language, "fr")
            .Add(component => component.LanguageChanged, code => picked = code)
            .Add(component => component.AppearanceChanged, _ => { })
            .Add(component => component.OnThemeOpen, () => { }));
        two.Find(".omni-app-menu__trigger").Click();
        Assert.Empty(two.FindAll(".omni-app-menu__flag"));
        // The drop-down posts the index of its option, as every OmniDropDown does.
        two.Find("select.omni-app-menu__language").Change("1");
        Assert.Equal("en", picked);
    }

    [Fact]
    public void The_mode_is_raised_and_the_theme_and_sign_out_close_the_menu_first()
    {
        OmniAppearance? mode = null;
        var events = new List<string>();
        var menu = Render<OmniAppMenu>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.Light)
            .Add(component => component.AppearanceChanged, value => mode = value)
            .Add(component => component.OnThemeOpen, () => events.Add("theme"))
            .Add(component => component.OnSignOut, () => events.Add("sign-out"))
            .Add(component => component.OpenChanged, open => events.Add(open ? "open" : "closed")));

        menu.Find(".omni-app-menu__trigger").Click();
        // One click on "Sombre" raises dark, without passing through system.
        menu.Find(".omni-app-menu__mode [role=radio][aria-label=Sombre]").Click();
        Assert.Equal(OmniAppearance.Dark, mode);

        menu.Find(".omni-app-menu__theme").Click();
        Assert.Equal(["open", "closed", "theme"], events);
        Assert.Empty(menu.FindAll(".omni-app-menu__card"));

        menu.Find(".omni-app-menu__trigger").Click();
        menu.Find(".omni-app-menu__sign-out").Click();
        Assert.Equal(["open", "closed", "theme", "open", "closed", "sign-out"], events);
    }

    [Fact]
    public void A_dark_only_theme_fixes_the_mode_on_dark_and_says_why()
    {
        OmniAppearance? mode = null;
        var menu = Render<OmniAppMenu>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.Light)
            .Add(component => component.Preset, (OmniThemePresets.All[0] with { DarkOnly = true }))
            .Add(component => component.AppearanceChanged, value => mode = value)
            .Add(component => component.OnThemeOpen, () => { }));
        menu.Find(".omni-app-menu__trigger").Click();

        var group = menu.Find(".omni-app-menu__mode [role=radiogroup]");
        Assert.Equal("Ce thème est toujours sombre", group.GetAttribute("title"));
        Assert.Equal("true", group.GetAttribute("aria-disabled"));
        var radios = menu.FindAll(".omni-app-menu__mode [role=radio]");
        Assert.Equal(3, radios.Count);
        Assert.All(radios, radio => Assert.True(radio.HasAttribute("disabled")));
        Assert.Equal(["false", "true", "false"], radios.Select(radio => radio.GetAttribute("aria-checked")));
        radios[0].Click();
        Assert.Null(mode);
    }

    [Theory]
    [InlineData(OmniAppearance.Light, "Clair", 0)]
    [InlineData(OmniAppearance.Dark, "Sombre", 1)]
    [InlineData(OmniAppearance.System, "Système", 2)]
    public void Each_mode_button_raises_its_own_mode_in_one_click_whatever_the_mode_in_force(OmniAppearance target, string name, int position)
    {
        foreach (var current in new[] { OmniAppearance.Light, OmniAppearance.Dark, OmniAppearance.System })
        {
            OmniAppearance? mode = null;
            var menu = Render<OmniAppMenu>(parameters => parameters
                .Add(component => component.Appearance, current)
                .Add(component => component.AppearanceChanged, value => mode = value)
                .Add(component => component.OnThemeOpen, () => { }));
            menu.Find(".omni-app-menu__trigger").Click();

            var radio = menu.FindAll(".omni-app-menu__mode [role=radio]")[position];
            Assert.Equal(name, radio.GetAttribute("aria-label"));
            radio.Click();

            Assert.Equal(target, mode);
        }
    }

    [Fact]
    public void The_mode_row_is_a_label_then_three_joined_icon_buttons_in_one_radio_group()
    {
        var menu = Render<OmniAppMenu>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.Dark)
            .Add(component => component.AppearanceChanged, _ => { })
            .Add(component => component.OnThemeOpen, () => { }));
        menu.Find(".omni-app-menu__trigger").Click();

        var row = menu.Find("div.omni-app-menu__mode");
        Assert.Empty(menu.FindAll("button.omni-app-menu__mode"));
        Assert.Equal("Mode", row.QuerySelector(".omni-app-menu__label")!.TextContent);
        // The label comes first and the group closes the row, so the buttons sit at its end.
        Assert.Same(row.QuerySelector(".omni-app-menu__label")!.NextElementSibling, row.QuerySelector("[role=radiogroup]"));
        Assert.Same(row.LastElementChild, row.QuerySelector("[role=radiogroup]"));

        var group = row.QuerySelector("[role=radiogroup]")!;
        Assert.Equal("Mode", group.GetAttribute("aria-label"));
        // The arrows are the page keys' (keys.js moves and picks inside a data-omni-roving group).
        Assert.True(group.HasAttribute("data-omni-roving"));
        Assert.Contains("omni-appearance-window__modes", group.ClassList);
        var radios = group.QuerySelectorAll("button[role=radio]");
        Assert.Equal(["Clair", "Sombre", "Système"], radios.Select(radio => radio.GetAttribute("aria-label")));
        Assert.Equal(["false", "true", "false"], radios.Select(radio => radio.GetAttribute("aria-checked")));
        Assert.Equal(["-1", "0", "-1"], radios.Select(radio => radio.GetAttribute("tabindex")));
        Assert.Equal(["omni-button--secondary", "omni-button--primary", "omni-button--secondary"],
            radios.Select(radio => radio.ClassList.Single(name => name is "omni-button--primary" or "omni-button--secondary")));
        Assert.All(radios, radio => Assert.False(radio.HasAttribute("disabled")));
        // Icon only: no text inside the buttons.
        Assert.All(radios, radio => Assert.Empty(radio.TextContent.Trim()));
    }

    [Fact]
    public void The_language_row_is_one_native_list_stretched_over_the_whole_row()
    {
        var menu = Render<OmniAppMenu>(parameters => parameters
            .Add(component => component.Languages, [new OmniAppMenuLanguage("fr", "Français", "flags/fr.svg"), new OmniAppMenuLanguage("en", "English")])
            .Add(component => component.Language, "fr")
            .Add(component => component.ShowFlags, true)
            .Add(component => component.AppearanceChanged, _ => { })
            .Add(component => component.OnThemeOpen, () => { }));
        menu.Find(".omni-app-menu__trigger").Click();

        // One actionable element in the row, named "Langue"; the icon, the label and the flag are not.
        var row = menu.Find(".omni-app-menu__language-row");
        var actionable = row.QuerySelectorAll("button, a, select, input, [role], [tabindex]");
        Assert.Single(actionable);
        Assert.Equal("select", actionable[0].LocalName);
        Assert.Equal("Langue", actionable[0].GetAttribute("aria-label"));

        // The stylesheet stretches the list over the row, and lets the click through the rest.
        var list = ShippedLookTests.Body(".omni-app-menu .omni-app-menu__language-row > select.omni-app-menu__language.omni-input");
        Assert.Equal("absolute", ShippedLookTests.Value(list, "position"));
        Assert.Equal("100%", ShippedLookTests.Value(list, "inline-size"));
        Assert.Equal("100%", ShippedLookTests.Value(list, "block-size"));
        Assert.Equal("none", ShippedLookTests.Value(ShippedLookTests.Body(".omni-app-menu__language-row > :is(.omni-icon, .omni-app-menu__label, .omni-app-menu__flag)"), "pointer-events"));
    }
}
