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
            .Add(component => component.OnTheme, () => { }));

        var trigger = menu.Find(".omni-app-menu__trigger");
        Assert.Equal("Menu de l’application", trigger.GetAttribute("aria-label"));
        Assert.Empty(menu.FindAll(".omni-app-menu__trigger-name"));
        trigger.Click();

        Assert.Empty(menu.FindAll(".omni-app-menu__identity"));
        Assert.Empty(menu.FindAll(".omni-app-menu__language"));
        Assert.Empty(menu.FindAll(".omni-app-menu__settings"));
        Assert.Empty(menu.FindAll(".omni-app-menu__footer"));
        Assert.Single(menu.FindAll("button.omni-app-menu__mode"));
        Assert.Empty(menu.FindAll("[role=radio], [role=radiogroup]"));
        Assert.Equal("Thème", menu.Find(".omni-app-menu__theme").TextContent.Trim());
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
            .Add(component => component.OnTheme, () => { })
            .Add(component => component.OnSettings, () => { })
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
            .Add(component => component.OnTheme, () => { }));
        single.Find(".omni-app-menu__trigger").Click();
        Assert.Empty(single.FindAll(".omni-app-menu__language"));

        string? picked = null;
        var two = Render<OmniAppMenu>(parameters => parameters
            .Add(component => component.Languages, [new OmniAppMenuLanguage("fr", "Français", "flags/fr.svg"), new OmniAppMenuLanguage("en", "English")])
            .Add(component => component.Language, "fr")
            .Add(component => component.LanguageChanged, code => picked = code)
            .Add(component => component.AppearanceChanged, _ => { })
            .Add(component => component.OnTheme, () => { }));
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
            .Add(component => component.OnTheme, () => events.Add("theme"))
            .Add(component => component.OnSignOut, () => events.Add("sign-out"))
            .Add(component => component.OpenChanged, open => events.Add(open ? "open" : "closed")));

        menu.Find(".omni-app-menu__trigger").Click();
        var row = menu.Find("button.omni-app-menu__mode");
        Assert.Equal("Clair", row.QuerySelector(".omni-app-menu__value")!.TextContent);
        row.Click();
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
            .Add(component => component.OnTheme, () => { }));
        menu.Find(".omni-app-menu__trigger").Click();

        var row = menu.Find("button.omni-app-menu__mode");
        Assert.True(row.HasAttribute("disabled"));
        Assert.Equal("Ce thème est toujours sombre", row.GetAttribute("title"));
        Assert.Equal("Sombre", row.QuerySelector(".omni-app-menu__value")!.TextContent);
        Assert.Null(mode);
    }

    [Theory]
    [InlineData(OmniAppearance.Light, OmniAppearance.Dark, "Clair")]
    [InlineData(OmniAppearance.Dark, OmniAppearance.System, "Sombre")]
    [InlineData(OmniAppearance.System, OmniAppearance.Light, "Système")]
    public void A_click_on_the_mode_row_moves_to_the_next_mode_and_the_cycle_comes_back_to_light(OmniAppearance current, OmniAppearance next, string shown)
    {
        OmniAppearance? mode = null;
        var menu = Render<OmniAppMenu>(parameters => parameters
            .Add(component => component.Appearance, current)
            .Add(component => component.AppearanceChanged, value => mode = value)
            .Add(component => component.OnTheme, () => { }));
        menu.Find(".omni-app-menu__trigger").Click();

        var row = menu.Find("button.omni-app-menu__mode");
        Assert.Equal(shown, row.QuerySelector(".omni-app-menu__value")!.TextContent);
        row.Click();

        Assert.Equal(next, mode);
    }

    [Fact]
    public void The_mode_row_is_one_button_named_by_its_text_with_nothing_interactive_inside()
    {
        var menu = Render<OmniAppMenu>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.Dark)
            .Add(component => component.AppearanceChanged, _ => { })
            .Add(component => component.OnTheme, () => { }));
        menu.Find(".omni-app-menu__trigger").Click();

        var row = menu.Find("button.omni-app-menu__mode");
        Assert.Equal("button", row.GetAttribute("type"));
        Assert.False(row.HasAttribute("aria-label"));
        Assert.Equal("Mode Sombre", string.Join(" ", row.QuerySelectorAll(".omni-app-menu__label, .omni-app-menu__value").Select(part => part.TextContent)));
        Assert.Empty(row.QuerySelectorAll("button, a, select, input, [role], [tabindex]"));
    }

    [Fact]
    public void The_language_row_is_one_native_list_stretched_over_the_whole_row()
    {
        var menu = Render<OmniAppMenu>(parameters => parameters
            .Add(component => component.Languages, [new OmniAppMenuLanguage("fr", "Français", "flags/fr.svg"), new OmniAppMenuLanguage("en", "English")])
            .Add(component => component.Language, "fr")
            .Add(component => component.ShowFlags, true)
            .Add(component => component.AppearanceChanged, _ => { })
            .Add(component => component.OnTheme, () => { }));
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
