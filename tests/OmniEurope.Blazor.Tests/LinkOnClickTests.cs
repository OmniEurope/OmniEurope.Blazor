using Bunit;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary><see cref="OmniLink.OnClick"/>: an action run with the navigation, and nothing without it.</summary>
public sealed class LinkOnClickTests : OmniBunitContext
{
    [Fact]
    public void OnClick_RunsOnClick_AndTheLinkStillNavigates()
    {
        MouseEventArgs? received = null;
        var link = Render<OmniLink>(parameters => parameters
            .Add(component => component.Href, "documentation")
            .Add(component => component.OnClick, args => received = args)
            .AddChildContent("Documentation"));

        link.Find("a").Click(new MouseEventArgs { CtrlKey = true });

        Assert.NotNull(received);
        Assert.True(received!.CtrlKey);
        Assert.Equal("documentation", link.Find("a").GetAttribute("href"));
    }

    [Fact]
    public void WithoutOnClick_NoHandlerIsAttached()
    {
        var link = Render<OmniLink>(parameters => parameters
            .Add(component => component.Href, "documentation")
            .AddChildContent("Documentation"));

        Assert.Throws<MissingEventHandlerException>(() => link.Find("a").Click());
    }
}
