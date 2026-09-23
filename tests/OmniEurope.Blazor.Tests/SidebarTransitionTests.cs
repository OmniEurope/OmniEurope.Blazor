using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A smooth pushing sidebar stays in the page when closed so its width can slide, and is still out of
/// reach; the instant one keeps disappearing as before.
/// </summary>
public sealed class SidebarTransitionTests : OmniBunitContext
{
    [Fact]
    public void The_default_sidebar_keeps_an_icon_rail_and_animates_push()
    {
        var sidebar = Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Open, false)
            .AddChildContent("Menu"));

        var aside = sidebar.Find("aside");
        Assert.False(aside.HasAttribute("hidden"));
        Assert.Contains("omni-sidebar--collapse-icons", aside.ClassName, StringComparison.Ordinal);
        Assert.Contains("omni-sidebar--smooth", aside.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void A_closed_instant_sidebar_is_removed_from_the_page()
    {
        var sidebar = Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Open, false)
            .Add(component => component.Collapse, OmniSidebarCollapse.Hidden)
            .Add(component => component.Transition, OmniSidebarTransition.Instant)
            .AddChildContent("Menu"));

        var aside = sidebar.Find("aside");
        Assert.True(aside.HasAttribute("hidden"));
        Assert.DoesNotContain("omni-sidebar--smooth", aside.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void A_closed_smooth_sidebar_stays_in_the_page_but_out_of_reach()
    {
        var sidebar = Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Open, false)
            .Add(component => component.Collapse, OmniSidebarCollapse.Hidden)
            .Add(component => component.Transition, OmniSidebarTransition.Smooth)
            .AddChildContent("Menu"));

        var aside = sidebar.Find("aside");
        Assert.False(aside.HasAttribute("hidden"));
        Assert.True(aside.HasAttribute("inert"));
        Assert.Equal("true", aside.GetAttribute("aria-hidden"));
        Assert.Contains("omni-sidebar--smooth", aside.ClassName, StringComparison.Ordinal);

        sidebar.Render(parameters => parameters.Add(component => component.Open, true));
        aside = sidebar.Find("aside");
        Assert.False(aside.HasAttribute("inert"));
        Assert.Null(aside.GetAttribute("aria-hidden"));
    }

    [Fact]
    public void The_smooth_transition_leaves_a_floating_sidebar_alone()
    {
        var sidebar = Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Open, false)
            .Add(component => component.Collapse, OmniSidebarCollapse.Hidden)
            .Add(component => component.Reveal, OmniSidebarReveal.Overlay)
            .Add(component => component.Transition, OmniSidebarTransition.Smooth)
            .AddChildContent("Menu"));

        var aside = sidebar.Find("aside");
        Assert.True(aside.HasAttribute("hidden"));
        Assert.DoesNotContain("omni-sidebar--smooth", aside.ClassName, StringComparison.Ordinal);
    }
}
