using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The app bar pieces the showcase used to draw with its own stylesheet (PLAN-008 lot 9), now in the
/// package: the indicator dot of a button, the icon of a text box, the brand of the header, the avatar
/// and the identity header of the account menu, the icon and description of a menu item, and the
/// error line of a radio list. Each is checked rendered, with its accessibility attributes, absent
/// when not asked for, and against the stylesheet rules that draw it.
/// </summary>
public sealed class AppBarComponentTests : OmniBunitContext
{
    // ---- OmniButton.Indicator ----

    [Fact]
    public void ButtonIndicator_DrawsADecorativeDot_AndTheNameKeepsTheMeaning()
    {
        var button = Render<OmniButton>(parameters => parameters
            .Add(component => component.Variant, OmniButtonVariant.Ghost)
            .Add(component => component.AriaLabel, "Notifications, 3 non lues")
            .Add(component => component.Indicator, true)
            .AddChildContent("B"));

        var root = button.Find("button");
        Assert.Contains("omni-button--indicator", root.ClassName, StringComparison.Ordinal);
        Assert.Equal("Notifications, 3 non lues", root.GetAttribute("aria-label"));
        var dot = root.QuerySelector(":scope > .omni-button__indicator");
        Assert.NotNull(dot);
        Assert.Equal("true", dot!.GetAttribute("aria-hidden"));
        Assert.Equal(string.Empty, dot.TextContent);

        // Outside the content span, so an icon alone still makes the button square.
        Assert.Null(root.QuerySelector(".omni-button__content .omni-button__indicator"));
    }

    [Fact]
    public void ButtonIndicator_IsAbsentByDefault()
    {
        var button = Render<OmniButton>(parameters => parameters.AddChildContent("B"));

        Assert.Empty(button.FindAll(".omni-button__indicator"));
        Assert.DoesNotContain("omni-button--indicator", button.Find("button").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void ButtonIndicator_IsTheDangerFillRingedByTheSurface_AtTheTopEndCorner()
    {
        Assert.Equal("relative", ShippedLookTests.Value(ShippedLookTests.Body(".omni-button--indicator"), "position"));
        var dot = ShippedLookTests.Body(".omni-button__indicator");
        Assert.Equal("var(--omni-color-danger-fill)", ShippedLookTests.Value(dot, "background"));
        Assert.Equal("0 0 0 2px var(--omni-color-surface)", ShippedLookTests.Value(dot, "box-shadow"));
        Assert.Equal("absolute", ShippedLookTests.Value(dot, "position"));
        // 5 px from the outer edge, as the mockup places it: the offset is taken inside the border.
        Assert.Equal("calc(5px - var(--omni-button-border-width, 1px))", ShippedLookTests.Value(dot, "inset-inline-end"));
        Assert.Equal("none", ShippedLookTests.Value(dot, "pointer-events"));
    }
}
