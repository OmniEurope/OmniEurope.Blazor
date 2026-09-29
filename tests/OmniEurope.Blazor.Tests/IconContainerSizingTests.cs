using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The <c>Icon</c> of <see cref="OmniBadge"/> and <see cref="OmniSplitButton"/> is a fragment, like on the
/// other components: the container sizes an icon that has no size of its own through
/// <c>--omni-icon-size</c>, and a <see cref="OmniIcon.Size"/> (or a class of the consumer) wins.
/// </summary>
public sealed class IconContainerSizingTests : OmniBunitContext
{
    [Fact]
    public void AnIconWithoutSize_ReadsTheContainerSizeAtNoSpecificity()
    {
        var rule = Assert.Single(ShippedLookTests.Rules(), rule => rule.Selector == ":where(.omni-icon)");
        Assert.Equal("var(--omni-icon-size, 1.25rem)", ShippedLookTests.Value(rule.Body, "height"));
        Assert.Equal("var(--omni-icon-size, 1.25rem)", ShippedLookTests.Value(rule.Body, "width"));
    }

    [Theory]
    [InlineData(".omni-badge", "0.875rem")]
    [InlineData(".omni-split-button--small > .omni-split-button__main", "1rem")]
    [InlineData(".omni-button--small", "1rem")]
    public void Containers_HandTheirIconSizeDown_InsteadOfSizingTheIcon(string container, string size)
    {
        var declared = ShippedLookTests.Rules()
            .Where(rule => rule.Selector == container && rule.Body.Contains("--omni-icon-size", StringComparison.Ordinal))
            .Select(rule => ShippedLookTests.Value(rule.Body, "--omni-icon-size"));
        Assert.Equal([size], declared);

        // A rule that sized the icon itself from the container would beat the icon's own Size class.
        Assert.DoesNotContain(ShippedLookTests.Rules(), rule => rule.Selector.StartsWith(container + " .omni-icon", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(".omni-icon--small", "1rem")]
    [InlineData(".omni-icon--medium", "1.25rem")]
    [InlineData(".omni-icon--large", "1.5rem")]
    public void AnExplicitSize_IsAFixedLength(string sizeClass, string length)
    {
        var body = ShippedLookTests.Body(sizeClass);
        Assert.Equal(length, ShippedLookTests.Value(body, "height"));
        Assert.Equal(length, ShippedLookTests.Value(body, "width"));
    }

    [Fact]
    public void Icon_WithoutSize_RendersNoSizeClass_AndWithSize_RendersIt()
    {
        var automatic = Render<OmniIcon>(parameters => parameters.Add(component => component.Name, OmniIconName.Check));
        var fixedSize = Render<OmniIcon>(parameters => parameters
            .Add(component => component.Name, OmniIconName.Check)
            .Add(component => component.Size, OmniControlSize.Medium));

        Assert.Equal(["omni-icon"], automatic.Find("svg").ClassList);
        Assert.Equal(["omni-icon", "omni-icon--medium"], fixedSize.Find("svg").ClassList);
    }

    [Fact]
    public void Badge_DrawsItsIconFragmentBeforeTheText()
    {
        var badge = Render<OmniBadge>(parameters => parameters
            .AddChildContent("Publié")
            .Add(component => component.Icon, Icon(OmniIconName.Check, size: null)));

        var root = badge.Find(".omni-badge");
        Assert.Equal("svg", root.FirstElementChild?.LocalName);
        Assert.Equal(["omni-icon"], root.FirstElementChild!.ClassList);
        Assert.Equal("Publié", root.TextContent.Trim());
        Assert.DoesNotContain("omni-badge--icon", root.ClassList);
    }

    [Fact]
    public void Badge_WithOnlyAnIcon_IsTheSquareIconBadge_AndKeepsAnExplicitSize()
    {
        var badge = Render<OmniBadge>(parameters => parameters
            .Add(component => component.Icon, Icon(OmniIconName.Check, OmniControlSize.Large)));

        var root = badge.Find(".omni-badge");
        Assert.Contains("omni-badge--icon", root.ClassList);
        Assert.Contains("omni-icon--large", root.QuerySelector("svg")!.ClassList);
    }

    private static RenderFragment Icon(OmniIconName name, OmniControlSize? size) => builder =>
    {
        builder.OpenComponent<OmniIcon>(0);
        builder.AddComponentParameter(1, nameof(OmniIcon.Name), name);
        if (size is { } value)
        {
            builder.AddComponentParameter(2, nameof(OmniIcon.Size), value);
        }

        builder.CloseComponent();
    };
}
