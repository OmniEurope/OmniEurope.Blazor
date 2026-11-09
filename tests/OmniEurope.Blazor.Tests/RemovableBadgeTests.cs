using Bunit;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A removable badge, so that a list of chips needs no local markup: the remove button, its localized name,
/// the callback, and the plain badge left untouched. The focus ring and the look are proved in a real browser.
/// </summary>
public sealed class RemovableBadgeTests : OmniBunitContext
{
    [Fact]
    public void ANonRemovableBadge_DrawsNoButton()
    {
        var badge = Render<OmniBadge>(parameters => parameters.AddChildContent("Paris"));

        Assert.Empty(badge.FindAll("button"));
        Assert.DoesNotContain("omni-badge--removable", badge.Find(".omni-badge").ClassList);
    }

    [Fact]
    public void ARemovableBadge_EndsWithARealButton_AndClickInvokesOnRemove()
    {
        var removed = 0;
        var badge = Render<OmniBadge>(parameters => parameters
            .Add(component => component.Removable, true)
            .Add(component => component.OnRemove, () => removed++)
            .AddChildContent("Paris"));

        var root = badge.Find(".omni-badge");
        Assert.Equal("SPAN", root.TagName);
        Assert.Contains("omni-badge--removable", root.ClassList);
        var button = badge.Find("button.omni-badge__remove");
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal("omni-badge", button.ParentElement!.ClassList.First(name => name == "omni-badge"));

        button.Click();
        Assert.Equal(1, removed);
    }

    [Fact]
    public void ARemovableBadge_NameIsLocalized_FromLabelOrFromTheText()
    {
        var named = Render<OmniBadge>(parameters => parameters
            .Add(component => component.Removable, true)
            .Add(component => component.Label, "Paris")
            .AddChildContent("Paris"));
        var plain = Render<OmniBadge>(parameters => parameters
            .Add(component => component.Removable, true)
            .AddChildContent("Rome"));
        var custom = Render<OmniBadge>(parameters => parameters
            .Add(component => component.Removable, true)
            .Add(component => component.RemoveLabel, "Drop Oslo")
            .AddChildContent("Oslo"));

        Assert.Equal("Retirer Paris", named.Find("button").GetAttribute("aria-label"));
        Assert.Null(named.Find("button").GetAttribute("aria-labelledby"));

        var plainButton = plain.Find("button");
        var textId = plain.Find(".omni-badge__text").Id;
        Assert.Equal("Retirer", plainButton.GetAttribute("aria-label"));
        Assert.Equal($"{plainButton.Id} {textId}", plainButton.GetAttribute("aria-labelledby"));

        Assert.Equal("Drop Oslo", custom.Find("button").GetAttribute("aria-label"));
    }

    [Theory]
    [InlineData("Delete", 1)]
    [InlineData("Backspace", 1)]
    [InlineData("a", 0)]
    public void DeleteAndBackspace_OnTheButton_AlsoRemove(string key, int expected)
    {
        var removed = 0;
        var badge = Render<OmniBadge>(parameters => parameters
            .Add(component => component.Removable, true)
            .Add(component => component.OnRemove, () => removed++)
            .AddChildContent("Paris"));

        badge.Find("button").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.Equal(expected, removed);
    }

    [Fact]
    public void ARemovableBadge_CannotAlsoBeAButtonOrALink()
    {
        Assert.Throws<ArgumentException>(() => Render<OmniBadge>(parameters => parameters
            .Add(component => component.Removable, true)
            .Add(component => component.OnClick, () => { })
            .AddChildContent("x")));
        Assert.Throws<ArgumentException>(() => Render<OmniBadge>(parameters => parameters
            .Add(component => component.Removable, true)
            .Add(component => component.Href, "/x")
            .AddChildContent("x")));
    }
}
