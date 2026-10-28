using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A client application's acceptance review: a badge that is never narrower than it is tall (R-5), a badge
/// told from a button (R-3) and a count that opens what it counts, without an <see cref="OmniButton"/>
/// wrapped around an <see cref="OmniBadge"/> (R-67). The markup, the callback, the refusals and the shipped
/// rules are proved here; the measured sizes, the hover and the focus ring are proved in a real browser.
/// </summary>
public sealed class ClickableBadgeTests : OmniBunitContext
{
    [Fact]
    public void AStaticBadge_StaysASpan_WithoutActionClassOrAriaLabel()
    {
        var badge = Render<OmniBadge>(parameters => parameters
            .Add(component => component.AriaLabel, "ignored")
            .AddChildContent("Quota"));

        var root = badge.Find(".omni-badge");
        Assert.Equal("SPAN", root.TagName);
        Assert.DoesNotContain("omni-badge--action", root.ClassList);
        Assert.Null(root.GetAttribute("aria-label"));
        Assert.Null(root.GetAttribute("type"));
        Assert.Null(root.GetAttribute("href"));
    }

    [Fact]
    public void ABadge_WithOnClick_IsOneButton_AndClicksTheCallback()
    {
        var clicks = 0;
        var badge = Render<OmniBadge>(parameters => parameters
            .Add(component => component.Tone, OmniTone.Accent)
            .Add(component => component.OnClick, () => clicks++)
            .AddChildContent("2"));

        var root = badge.Find(".omni-badge");
        Assert.Equal("BUTTON", root.TagName);
        Assert.Equal("button", root.GetAttribute("type"));
        Assert.Contains("omni-badge--action", root.ClassList);
        Assert.Contains("omni-badge--accent", root.ClassList);
        Assert.Null(root.GetAttribute("href"));
        Assert.Null(root.GetAttribute("style"));

        root.Click();
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void ABadge_WithHref_IsOneLink_AndAriaLabelNamesIt()
    {
        var badge = Render<OmniBadge>(parameters => parameters
            .Add(component => component.Href, "/users")
            .Add(component => component.AriaLabel, "Used by 2 users")
            .AddChildContent("2"));

        var root = badge.Find(".omni-badge");
        Assert.Equal("A", root.TagName);
        Assert.Equal("/users", root.GetAttribute("href"));
        Assert.Equal("Used by 2 users", root.GetAttribute("aria-label"));
        Assert.Contains("omni-badge--action", root.ClassList);
    }

    [Fact]
    public void AClickableBadge_NamedByAriaLabel_OverTheHostsOwnAttribute_AndKeepsTheHostsWhenNone()
    {
        var named = Render<OmniBadge>(parameters => parameters
            .Add(component => component.OnClick, () => { })
            .Add(component => component.AriaLabel, "Open the list")
            .AddUnmatched("aria-label", "host")
            .AddChildContent("2"));
        Assert.Equal("Open the list", named.Find(".omni-badge").GetAttribute("aria-label"));

        var hosted = Render<OmniBadge>(parameters => parameters
            .Add(component => component.OnClick, () => { })
            .AddUnmatched("aria-label", "host")
            .AddChildContent("2"));
        Assert.Equal("host", hosted.Find(".omni-badge").GetAttribute("aria-label"));
    }

    [Fact]
    public void ABadge_WithoutAriaLabel_IsNamedByItsContent()
    {
        var badge = Render<OmniBadge>(parameters => parameters
            .Add(component => component.OnClick, () => { })
            .AddChildContent("2"));

        Assert.Null(badge.Find(".omni-badge").GetAttribute("aria-label"));
    }

    [Fact]
    public void ABadge_WithBothHrefAndOnClick_IsRefused()
    {
        Assert.Throws<ArgumentException>(() => Render<OmniBadge>(parameters => parameters
            .Add(component => component.Href, "/a")
            .Add(component => component.OnClick, () => { })
            .AddChildContent("x")));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,x")]
    [InlineData("/a\nb")]
    [InlineData("//evil.example")]
    public void ABadge_RefusesAnUnsafeHref(string href)
    {
        Assert.Throws<InvalidOperationException>(() => Render<OmniBadge>(parameters => parameters
            .Add(component => component.Href, href)
            .AddChildContent("x")));
    }

    [Fact]
    public void ABlankHref_LeavesTheBadgeASpan()
    {
        var badge = Render<OmniBadge>(parameters => parameters
            .Add(component => component.Href, " ")
            .AddChildContent("x"));

        Assert.Equal("SPAN", badge.Find(".omni-badge").TagName);
    }

    [Fact]
    public void ABadge_IsNeverNarrowerThanItIsTall_AndTheWidthStepsComeAfterTheFloor()
    {
        var css = ShippedLookTests.Css;
        var floor = css.IndexOf(".omni-badge { min-inline-size: max(var(--omni-badge-height)", StringComparison.Ordinal);
        var step = css.IndexOf(".omni-badge--width-small { min-inline-size:", StringComparison.Ordinal);
        Assert.True(floor >= 0, "The badge has no width floor tied to its height.");
        Assert.True(step > floor, "A Width step must come after the floor, so that it raises it and never lowers it.");
        Assert.Equal("var(--omni-badge-height)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-badge"), "min-height"));
        Assert.Equal("var(--omni-badge-height)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-badge"), "min-width"));
    }

    [Fact]
    public void AClickableBadge_HasAFocusRing_AndAHoverThatIsNotAButtonsFillChange()
    {
        var css = ShippedLookTests.Css;
        Assert.Contains(".omni-badge--action:focus-visible { box-shadow: var(--omni-focus-ring);", css, StringComparison.Ordinal);
        var hover = ShippedLookTests.Body(".omni-badge.omni-badge--action:hover");
        Assert.Contains("text-decoration: underline", hover, StringComparison.Ordinal);
        Assert.DoesNotContain("transform", hover, StringComparison.Ordinal);
        Assert.DoesNotContain("box-shadow", hover, StringComparison.Ordinal);
    }
}
