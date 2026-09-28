using Bunit;
using Microsoft.AspNetCore.Components;
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
            .Add(component => component.Icon, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniIcon>(0);
                builder.AddAttribute(1, nameof(OmniIcon.Name), OmniIconName.Check);
                builder.CloseComponent();
            })));

        var marker = item.Find(".omni-steps__number");
        Assert.NotNull(marker.QuerySelector(".omni-steps__icon[aria-hidden=true] svg"));
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
