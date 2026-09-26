using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary><see cref="OmniStepsItem.Icon"/>: an icon in the marker, the number kept for assistive technologies.</summary>
public sealed class StepsItemIconTests : OmniBunitContext
{
    [Fact]
    public void Icon_ReplacesTheVisibleNumber_AndKeepsItForAssistiveTechnologies()
    {
        var item = Render<OmniStepsItem>(parameters => parameters
            .Add(component => component.Index, 1)
            .Add(component => component.Title, "Validation")
            .Add(component => component.Icon, OmniIconName.Check));

        var marker = item.Find(".omni-steps__number");
        Assert.NotNull(marker.QuerySelector("svg"));
        Assert.Equal("2", marker.QuerySelector(".omni-visually-hidden")!.TextContent);
    }

    [Fact]
    public void WithoutIcon_TheMarkerShowsTheNumberAlone()
    {
        var item = Render<OmniStepsItem>(parameters => parameters
            .Add(component => component.Index, 0)
            .Add(component => component.Title, "Saisie"));

        var marker = item.Find(".omni-steps__number");
        Assert.Equal("1", marker.TextContent);
        Assert.Null(marker.QuerySelector("svg"));
    }
}
