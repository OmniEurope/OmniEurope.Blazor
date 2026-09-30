using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A group clicked on the rail opens the sidebar on that group instead of unfolding out of sight, and
/// the header's brand can lead home.
/// </summary>
public sealed class RailGroupAndBrandLinkTests : OmniBunitContext
{
    private IRenderedComponent<OmniSidebar> RenderRail(Action<bool>? openChanged)
    {
        return Render<OmniSidebar>(parameters =>
        {
            parameters
                .Add(component => component.Open, false)
                .Add(component => component.Collapse, OmniSidebarCollapse.Icons)
                .AddChildContent<OmniPanelMenu>(menu => menu
                    .AddChildContent<OmniPanelMenuItem>(group => group
                        .Add(component => component.Text, "Drive")
                        .Add(component => component.Href, "drive/modules")
                        .AddChildContent<OmniPanelMenuItem>(child => child
                            .Add(component => component.Text, "Appareils")
                            .Add(component => component.Href, "drive/devices"))));
            if (openChanged is not null)
            {
                parameters.Add(component => component.OpenChanged, openChanged);
            }
        });
    }

    [Fact]
    public void A_group_clicked_on_the_rail_opens_the_sidebar_on_that_group_unfolded()
    {
        bool? requested = null;
        var sidebar = RenderRail(open => requested = open);

        sidebar.Find(".omni-panel-menu__summary .omni-panel-menu__link").Click();

        Assert.True(requested);
        Assert.Contains("omni-panel-menu__group--open", sidebar.Find(".omni-panel-menu__group").ClassList);
    }

    [Fact]
    public void Without_a_way_to_open_the_sidebar_a_rail_group_still_unfolds_in_place()
    {
        var sidebar = RenderRail(null);

        sidebar.Find(".omni-panel-menu__summary .omni-panel-menu__link").Click();

        Assert.Contains("omni-panel-menu__group--open", sidebar.Find(".omni-panel-menu__group").ClassList);
    }

    [Fact]
    public void An_open_sidebar_group_click_only_toggles_it()
    {
        bool? requested = null;
        var sidebar = Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.OpenChanged, (bool open) => requested = open)
            .AddChildContent<OmniPanelMenu>(menu => menu
                .AddChildContent<OmniPanelMenuItem>(group => group
                    .Add(component => component.Text, "Drive")
                    .AddChildContent<OmniPanelMenuItem>(child => child
                        .Add(component => component.Text, "Appareils")
                        .Add(component => component.Href, "drive/devices")))));

        sidebar.Find(".omni-panel-menu__summary .omni-panel-menu__link").Click();

        Assert.Null(requested);
        Assert.Contains("omni-panel-menu__group--open", sidebar.Find(".omni-panel-menu__group").ClassList);
    }

    [Fact]
    public void A_brand_with_an_address_is_one_link_holding_the_logo_image_and_the_name()
    {
        var header = Render<OmniHeader>(parameters => parameters
            .Add(component => component.Brand, "Atelier")
            .Add(component => component.BrandLogo, "img/logo.svg")
            .Add(component => component.BrandHref, "tasks")
            .AddChildContent("actions"));

        var link = header.Find("a.omni-header__brand");
        Assert.Equal("tasks", link.GetAttribute("href"));
        Assert.Equal("img/logo.svg", link.QuerySelector("img.omni-header__logo-image")!.GetAttribute("src"));
        Assert.Equal("Atelier", link.QuerySelector(".omni-header__brand-name")!.TextContent);
    }

    [Fact]
    public void Appearance_settings_pair_their_rows_two_by_two_mode_beside_font()
    {
        // The track floor is at least half the width: two 30rem columns side by side, a third never fits.
        var grid = ShippedLookTests.Body(".omni-appearance-settings:not(.omni-appearance-settings--compact, .omni-appearance-settings--window)");
        Assert.Equal(
            "repeat(auto-fit, minmax(min(100%, max(30rem, calc((100% - var(--omni-space-md)) / 2))), 1fr))",
            ShippedLookTests.Value(grid, "grid-template-columns"));

        // Mode then font, then the row that opens the window of the theme, the palette and the scales.
        var settings = Render<OmniAppearanceSettings>();
        var labels = settings.FindAll(".omni-appearance-settings > .omni-appearance-settings__row > .omni-appearance-settings__label")
            .Select(label => label.TextContent.Trim())
            .ToArray();
        Assert.Equal(["Mode", "Police", "Thème, palette et tailles"], labels);
    }

    [Fact]
    public void A_brand_without_an_address_stays_a_label()
    {
        var header = Render<OmniHeader>(parameters => parameters
            .Add(component => component.Brand, "Atelier")
            .AddChildContent("actions"));

        Assert.Empty(header.FindAll("a.omni-header__brand"));
        Assert.Equal("SPAN", header.Find(".omni-header__brand").TagName);
    }
}
