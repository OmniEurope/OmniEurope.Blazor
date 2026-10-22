using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The separator drawn between blocks (request R2-24 of a client application): one element with the
/// separator role and the orientation of its rule, no style attribute, and the host's own class and
/// attributes. The measured look (one pixel, no margin, stretching inside a stack) is proved in a real
/// browser, not here.
/// </summary>
public sealed class DividerTests : OmniBunitContext
{
    [Fact]
    public void ADivider_IsAHorizontalSeparator_ByDefault()
    {
        var divider = Render<OmniDivider>();

        var element = divider.Find("div");
        Assert.Equal("separator", element.GetAttribute("role"));
        Assert.Equal("horizontal", element.GetAttribute("aria-orientation"));
        Assert.Equal(OmniOrientation.Horizontal, divider.Instance.Orientation);
        Assert.Contains("omni-divider", element.ClassList);
        Assert.Contains("omni-divider--horizontal", element.ClassList);
        Assert.DoesNotContain("omni-divider--vertical", element.ClassList);
        Assert.Empty(element.InnerHtml);
    }

    [Fact]
    public void AVerticalDivider_ReportsAVerticalSeparator()
    {
        var divider = Render<OmniDivider>(parameters => parameters.Add(component => component.Orientation, OmniOrientation.Vertical));

        var element = divider.Find("div");
        Assert.Equal("separator", element.GetAttribute("role"));
        Assert.Equal("vertical", element.GetAttribute("aria-orientation"));
        Assert.Contains("omni-divider--vertical", element.ClassList);
        Assert.DoesNotContain("omni-divider--horizontal", element.ClassList);
    }

    [Fact]
    public void ADividerWithAnUndefinedOrientation_IsDrawnHorizontal()
    {
        var divider = Render<OmniDivider>(parameters => parameters.Add(component => component.Orientation, (OmniOrientation)99));

        var element = divider.Find("div");
        Assert.Equal("horizontal", element.GetAttribute("aria-orientation"));
        Assert.Contains("omni-divider--horizontal", element.ClassList);
    }

    [Fact]
    public void ADivider_KeepsTheHostClassIdAndAttributes_AndHasNoInlineStyle()
    {
        var divider = Render<OmniDivider>(parameters => parameters
            .Add(component => component.Class, "mine")
            .Add(component => component.Id, "rule")
            .AddUnmatched("data-probe", "x"));

        var element = divider.Find("div");
        Assert.Contains("mine", element.ClassList);
        Assert.Contains("omni-divider", element.ClassList);
        Assert.Equal("rule", element.Id);
        Assert.Equal("x", element.GetAttribute("data-probe"));
        Assert.False(element.HasAttribute("style"));
        Assert.Equal("separator", element.GetAttribute("role"));
    }

    [Fact]
    public void ADividerInAStack_IsOneChildOfTheStack()
    {
        var stack = Render<OmniStack>(parameters => parameters.AddChildContent<OmniDivider>());

        var child = Assert.Single(stack.Find(".omni-stack").Children);
        Assert.Equal("separator", child.GetAttribute("role"));
    }
}
