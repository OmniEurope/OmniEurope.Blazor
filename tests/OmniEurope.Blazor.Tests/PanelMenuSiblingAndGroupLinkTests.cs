using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Two panel menu behaviours the route cannot decide alone: clicking the label of a group that names
/// a page unfolds it as well as navigating, and among siblings whose addresses prefix one another
/// only the deepest match is the current page.
/// </summary>
public sealed class PanelMenuSiblingAndGroupLinkTests : OmniBunitContext
{
    private const string Open = "omni-panel-menu__group--open";
    private const string Closed = "omni-panel-menu__group--closed";
    private const string GroupLabel = "#group-servers > .omni-panel-menu__summary > a";
    private const string GroupToggle = "#group-servers > .omni-panel-menu__summary > .omni-panel-menu__toggle";

    [Fact]
    public async Task ClickingTheLabelOfAGroupWithAnAddress_NavigatesAndUnfoldsIt()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        var host = Render<PanelMenuGroupLinkTestHost>(parameters => parameters
            .Add(component => component.CanNavigate, _ => Task.FromResult(true)));
        Assert.Contains(Closed, host.Find("#group-servers").ClassName);

        await host.Find(GroupLabel).ClickAsync(new MouseEventArgs());

        Assert.EndsWith("/servers", navigation.Uri, StringComparison.Ordinal);
        Assert.Contains(Open, host.Find("#group-servers").ClassName);
        Assert.Equal("true", host.Find(GroupToggle).GetAttribute("aria-expanded"));
    }

    [Fact]
    public async Task ClickingTheLabelOfAnUnfoldedGroup_KeepsItUnfolded()
    {
        var host = Render<PanelMenuGroupLinkTestHost>(parameters => parameters
            .Add(component => component.CanNavigate, _ => Task.FromResult(true)));
        host.Find(GroupToggle).Click();
        Assert.Contains(Open, host.Find("#group-servers").ClassName);

        await host.Find(GroupLabel).ClickAsync(new MouseEventArgs());

        Assert.Contains(Open, host.Find("#group-servers").ClassName);
    }

    [Fact]
    public void TheChevronOfAGroupWithAnAddress_TogglesWithoutNavigating()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        var start = navigation.Uri;
        var host = Render<PanelMenuGroupLinkTestHost>(parameters => parameters
            .Add(component => component.CanNavigate, _ => Task.FromResult(true)));

        host.Find(GroupToggle).Click();
        Assert.Contains(Open, host.Find("#group-servers").ClassName);
        host.Find(GroupToggle).Click();
        Assert.Contains(Closed, host.Find("#group-servers").ClassName);

        Assert.Equal(start, navigation.Uri);
    }

    [Theory]
    [InlineData("/settings/users", "leaf-users")]
    [InlineData("/settings/users/42", "leaf-users")]
    [InlineData("/settings", "leaf-settings")]
    [InlineData("/settings/profile", "leaf-settings")]
    [InlineData("/reports/yearly", "leaf-yearly")]
    [InlineData("/reports", "leaf-reports")]
    [InlineData("/reports/monthly", "leaf-reports")]
    public void AmongSiblingsWhoseAddressesPrefixOneAnother_OnlyTheDeepestMatchIsCurrent(string route, string expected)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(route);

        var host = Render<PanelMenuSiblingsTestHost>();

        host.WaitForAssertion(() =>
        {
            var current = host.FindAll("[aria-current='page']");
            Assert.Single(current);
            Assert.Equal(expected, current[0].Id);
        });
    }

    [Fact]
    public void TheCurrentSibling_FollowsTheRouteAsItMoves()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/settings");
        var host = Render<PanelMenuSiblingsTestHost>();
        host.WaitForAssertion(() => Assert.Equal("page", host.Find("#leaf-settings").GetAttribute("aria-current")));

        navigation.NavigateTo("/settings/users");

        host.WaitForAssertion(() =>
        {
            Assert.Null(host.Find("#leaf-settings").GetAttribute("aria-current"));
            Assert.Equal("page", host.Find("#leaf-users").GetAttribute("aria-current"));
        });
    }
}
