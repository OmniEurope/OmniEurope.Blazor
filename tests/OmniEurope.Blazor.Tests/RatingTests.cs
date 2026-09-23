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
    public void Readonly_rating_keeps_five_visible_stars_without_editable_actions()
    {
        int? value = 3;
        var rating = Render<OmniRating>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ReadOnly, true));
        Assert.Equal(5, rating.FindAll("button:disabled").Count);
    }
}
