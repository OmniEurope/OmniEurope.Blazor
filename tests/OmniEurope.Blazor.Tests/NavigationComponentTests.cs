using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class NavigationComponentTests : BunitContext
{
    [Fact]
    public void StructuredNavigation_RendersSemanticCurrentStates()
    {
        var host = Render<NavigationTestHost>();

        Assert.Equal("NAV", host.Find(".omni-breadcrumb").TagName);
        Assert.Equal("page", host.Find(".omni-breadcrumb [aria-current]").GetAttribute("aria-current"));
        Assert.Equal("page", host.Find(".omni-panel-menu [aria-current]").GetAttribute("aria-current"));
        Assert.Equal("true", host.Find(".omni-tabs__tab").GetAttribute("aria-selected"));
        Assert.DoesNotContain("style=", host.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TabsAndSteps_AreControlledAndRespectNavigationValidation()
    {
        var host = Render<NavigationTestHost>();

        host.Find(".omni-tabs").KeyDown("ArrowRight");
        Assert.Equal("second", host.Instance.Tab);
        Assert.Contains("Contenu 2", host.Find(".omni-tabs__panel:not([hidden])").TextContent, StringComparison.Ordinal);

        host.Instance.AllowStep = false;
        host.FindAll(".omni-steps__button")[1].Click();
        Assert.Equal(0, host.Instance.Step);

        host.Instance.AllowStep = true;
        host.FindAll(".omni-steps__button")[1].Click();
        Assert.Equal(1, host.Instance.Step);
    }

    [Fact]
    public void ProfileMenu_InvokesItsAction()
    {
        var host = Render<NavigationTestHost>();

        host.Find(".omni-profile-menu button").Click();

        Assert.True(host.Instance.SignedOut);
        Assert.Equal("menu", host.Find(".omni-profile-menu__items").GetAttribute("role"));
    }

    [Fact]
    public void PanelMenu_CanCancelRouteNavigation()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        var initial = navigation.Uri;
        var item = Render<OmniPanelMenuItem>(parameters => parameters
            .Add(component => component.Text, "Protégé")
            .Add(component => component.Href, "/protected")
            .Add(component => component.CanNavigate, _ => Task.FromResult(false)));

        item.Find("a").Click();

        Assert.Equal(initial, navigation.Uri);
    }
}
