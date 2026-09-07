using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The panel menu's disclosure state is derived, never stored on the consumer's side: the active
/// route opens the groups that contain it, and a hand toggle overrides that until the route moves
/// back in. These tests pin that cycle, which nothing else exercises.
/// </summary>
public sealed class PanelMenuGroupTests : OmniBunitContext
{
    private const string Open = "omni-panel-menu__group--open";
    private const string Closed = "omni-panel-menu__group--closed";

    [Fact]
    public void Groups_StayClosedWhileTheRouteIsOutsideThem()
    {
        var host = Render<PanelMenuGroupTestHost>();

        Assert.Contains(Closed, host.Find("#group-outer").ClassName);
        Assert.Contains(Closed, host.Find("#group-inner").ClassName);
        Assert.True(host.Find("#group-outer .omni-panel-menu__children").HasAttribute("hidden"));
    }

    [Fact]
    public void EveryGroupAboveTheCurrentPageUnfolds_IncludingTheOutermost()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/reports/detail");

        var host = Render<PanelMenuGroupTestHost>();

        host.WaitForAssertion(() =>
        {
            Assert.Contains(Open, host.Find("#group-inner").ClassName);
            Assert.Contains(Open, host.Find("#group-outer").ClassName);
        });
    }

    [Fact]
    public void NavigatingIntoAGroupUnfoldsEveryLevelAboveThePage()
    {
        var host = Render<PanelMenuGroupTestHost>();
        Assert.Contains(Closed, host.Find("#group-outer").ClassName);

        Services.GetRequiredService<NavigationManager>().NavigateTo("/reports/detail");

        host.WaitForAssertion(() =>
        {
            Assert.Contains(Open, host.Find("#group-inner").ClassName);
            Assert.Contains(Open, host.Find("#group-outer").ClassName);
        });
    }

    [Fact]
    public void AHandToggleOpensAndClosesAGroupTheRouteDoesNotTouch()
    {
        var host = Render<PanelMenuGroupTestHost>();
        var toggle = "#group-outer > .omni-panel-menu__summary > .omni-panel-menu__toggle";

        Assert.Equal("false", host.Find(toggle).GetAttribute("aria-expanded"));

        host.Find(toggle).Click();
        Assert.Contains(Open, host.Find("#group-outer").ClassName);
        Assert.Equal("true", host.Find(toggle).GetAttribute("aria-expanded"));

        host.Find(toggle).Click();
        Assert.Contains(Closed, host.Find("#group-outer").ClassName);
    }

    [Fact]
    public void AHandCollapsedGroupReopensWhenTheRouteEntersIt()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/reports/detail");
        var host = Render<PanelMenuGroupTestHost>();
        var toggle = "#group-outer > .omni-panel-menu__summary > .omni-panel-menu__toggle";
        host.WaitForAssertion(() => Assert.Contains(Open, host.Find("#group-outer").ClassName));

        // Collapsed by hand while the page it holds is still the current one.
        host.Find(toggle).Click();
        Assert.Contains(Closed, host.Find("#group-outer").ClassName);

        // Leaving does not undo the hand toggle, or collapsing a group would reopen on the next click.
        Services.GetRequiredService<NavigationManager>().NavigateTo("/elsewhere");
        host.WaitForAssertion(() => Assert.Contains(Closed, host.Find("#group-outer").ClassName));

        // Coming back in does, so navigating to a child always reveals it.
        Services.GetRequiredService<NavigationManager>().NavigateTo("/reports/detail");
        host.WaitForAssertion(() => Assert.Contains(Open, host.Find("#group-outer").ClassName));
    }
}
