using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary><see cref="OmniHeader.BrandContent"/>: the host's own mark, in the brand area next to the name.</summary>
public sealed class HeaderBrandContentTests : OmniBunitContext
{
    private static readonly RenderFragment Mark = builder =>
    {
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "class", "host-mark");
        builder.CloseElement();
    };

    [Fact]
    public void BrandContent_RendersInTheBrandArea_BeforeTheName()
    {
        var header = Render<OmniHeader>(parameters => parameters
            .Add(component => component.Brand, "Contoso")
            .Add(component => component.BrandContent, Mark)
            .AddChildContent("x"));

        var brand = header.Find(".omni-header__brand");
        var children = brand.Children.ToList();
        Assert.Equal(2, children.Count);
        Assert.Contains("omni-header__brand-content", children[0].ClassList);
        Assert.NotNull(children[0].QuerySelector("svg.host-mark"));
        Assert.Contains("omni-header__brand-name", children[1].ClassList);
        Assert.Equal("Contoso", children[1].TextContent);
    }

    [Fact]
    public void BrandContent_WinsOverBrandLogo_AndAloneStillMakesTheBrand()
    {
        var header = Render<OmniHeader>(parameters => parameters
            .Add(component => component.BrandLogo, "logo.png")
            .Add(component => component.BrandContent, Mark)
            .AddChildContent("x"));

        Assert.Empty(header.FindAll("img.omni-header__logo-image"));
        Assert.NotNull(header.Find(".omni-header__brand .host-mark"));
        Assert.Contains("omni-header--branded", header.Find("header").ClassList);
    }

    [Fact]
    public void BrandContent_GoesInsideTheHomeLink_WhenTheBrandLinksHome()
    {
        var header = Render<OmniHeader>(parameters => parameters
            .Add(component => component.Brand, "Contoso")
            .Add(component => component.BrandHref, "/")
            .Add(component => component.BrandLogo, "logo.png")
            .Add(component => component.BrandContent, Mark)
            .AddChildContent("x"));

        var link = header.Find("a.omni-header__brand--link");
        Assert.NotNull(link.QuerySelector(".omni-header__brand-content .host-mark"));
        Assert.Null(link.QuerySelector("img"));
    }

    [Fact]
    public void WithoutBrandContent_TheMarkupIsUnchanged()
    {
        var header = Render<OmniHeader>(parameters => parameters
            .Add(component => component.Brand, "Contoso")
            .Add(component => component.BrandLogo, "logo.png")
            .AddChildContent("x"));

        Assert.Empty(header.FindAll(".omni-header__brand-content"));
        Assert.Equal("logo.png", header.Find(".omni-header__brand img.omni-header__logo-image").GetAttribute("src"));
        Assert.Equal(2, header.Find(".omni-header__brand").Children.Length);
    }
}
