using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class RatingTests : OmniBunitContext
{
    [Fact]
    public void Selecting_a_star_fills_every_preceding_star_and_can_decrease_the_rating()
    {
        int? value = 1;
        var rating = Render<OmniRating>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ValueChanged, selected => value = selected));
        rating.FindAll("button")[3].Click();
        Assert.Equal(4, value);
        Assert.Equal("true", rating.FindAll("button")[3].GetAttribute("aria-pressed"));
        rating.FindAll("button")[1].Click();
        Assert.Equal(2, value);
        Assert.Equal(5, rating.FindAll("button").Count);
    }

    [Fact]
    public void Disabled_wins_over_ReadOnly()
    {
        int? value = 3;
        var rating = Render<OmniRating>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ReadOnly, true)
            .Add(component => component.Disabled, true));
        Assert.Equal(5, rating.FindAll("button:disabled").Count);
        Assert.Contains("omni-rating--disabled", rating.Find(".omni-rating").ClassList);
    }

    [Fact]
    public void ReadOnly_rating_is_one_focusable_announced_value_that_cannot_change()
    {
        int? value = 3;
        var rating = Render<OmniRating>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ValueChanged, selected => value = selected)
            .Add(component => component.Label, "Note")
            .Add(component => component.ReadOnly, true));

        var root = rating.Find(".omni-rating");
        Assert.Equal("img", root.GetAttribute("role"));
        Assert.Equal("0", root.GetAttribute("tabindex"));
        Assert.Equal("Note 3/5", root.GetAttribute("aria-label"));
        Assert.Contains("omni-rating--readonly", root.ClassList);
        Assert.DoesNotContain("omni-rating--disabled", root.ClassList);
        Assert.Empty(rating.FindAll("button"));
        var filled = OmniEurope.Blazor.Internal.PhosphorIconGlyphs.For(OmniIconName.StarFilled).PathData;
        Assert.Equal(3, rating.FindAll(".omni-rating__star path").Count(path => path.GetAttribute("d") == filled));
        Assert.Equal(5, rating.FindAll(".omni-rating__star").Count);
    }

    [Fact]
    public void Disabled_rating_keeps_disabled_star_buttons_out_of_the_tab_order()
    {
        int? value = 2;
        var rating = Render<OmniRating>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Label, "Note")
            .Add(component => component.Disabled, true));

        var root = rating.Find(".omni-rating");
        Assert.Equal("group", root.GetAttribute("role"));
        Assert.Null(root.GetAttribute("tabindex"));
        Assert.Contains("omni-rating--disabled", root.ClassList);
        Assert.Equal(5, rating.FindAll("button:disabled").Count);
    }
}
