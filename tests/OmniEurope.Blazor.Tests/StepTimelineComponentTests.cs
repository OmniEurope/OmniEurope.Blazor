using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

public sealed class StepTimelineComponentTests : OmniBunitContext
{
    private static readonly DateTimeOffset Run = new(2026, 3, 2, 9, 0, 0, TimeSpan.Zero);

    private static IReadOnlyList<OmniStepTimelineStep> Steps() =>
    [
        new("Prepare", Run, Run.AddSeconds(60), OmniStepTimelineStatus.Success, Secondary: true),
        new("Build", Run.AddSeconds(60), Run.AddSeconds(180), OmniStepTimelineStatus.Failed),
        new("Deploy", Run.AddSeconds(180), null, OmniStepTimelineStatus.Running),
        new("Confirm", null, null, OmniStepTimelineStatus.Skipped),
        new("Commit", null, null)
    ];

    [Fact]
    public void Layout_PlacesEachStepAtItsOffsetOnTheWholeRun_ARunningStepReachingNow()
    {
        var layout = StepTimelineLayout.Build(Steps(), Run.AddSeconds(240));

        Assert.Equal(TimeSpan.FromSeconds(240), layout.Total);
        Assert.Equal([(0d, 25d), (25d, 50d), (75d, 25d)], layout.Bars.Select(bar => (bar.OffsetPercent, bar.WidthPercent)));
        Assert.Equal(TimeSpan.FromSeconds(60), layout.Bars[2].Duration);
        Assert.Equal(["Confirm", "Commit"], layout.NeverStarted.Select(step => step.Name));
    }

    [Fact]
    public void Layout_KeepsAVeryShortStepVisible_AndInsideTheAxis()
    {
        var layout = StepTimelineLayout.Build(
        [
            new("Long", Run, Run.AddSeconds(1000)),
            new("Blink", Run.AddSeconds(1000), Run.AddSeconds(1000))
        ], Run);

        var blink = layout.Bars[1];
        Assert.Equal(StepTimelineLayout.MinimumWidthPercent, blink.WidthPercent);
        Assert.Equal(100 - StepTimelineLayout.MinimumWidthPercent, blink.OffsetPercent, 6);
    }

    [Fact]
    public void Layout_SurvivesARunWithNoLength_AndAnEndBeforeItsStart()
    {
        var instant = StepTimelineLayout.Build([new("Seule", Run, Run)], Run);
        var skewed = StepTimelineLayout.Build(
        [
            new("Avant", Run, Run.AddSeconds(100)),
            new("Horloge", Run.AddSeconds(50), Run.AddSeconds(40))
        ], Run);

        Assert.Equal((0d, 100d), (instant.Bars[0].OffsetPercent, instant.Bars[0].WidthPercent));
        Assert.Equal(TimeSpan.Zero, skewed.Bars[1].Duration);
        Assert.Equal(StepTimelineLayout.MinimumWidthPercent, skewed.Bars[1].WidthPercent);
    }

    [Theory]
    [InlineData(42, "42s")]
    [InlineData(185, "3m05")]
    [InlineData(1215, "20m15")]
    [InlineData(4800, "1h20")]
    public void Durations_ReadLikeARunLog(int seconds, string expected) =>
        Assert.Equal(expected, StepTimelineLayout.Format(TimeSpan.FromSeconds(seconds)));

    [Fact]
    public void Render_DrawsARowPerStartedStep_WithItsBarItsDurationAndItsStatus()
    {
        var timeline = Render<OmniStepTimeline>(parameters => parameters
            .Add(component => component.Steps, Steps())
            .Add(component => component.Now, Run.AddSeconds(240)));

        var rows = timeline.FindAll(".omni-step-timeline__step");
        Assert.Equal(3, rows.Count);
        Assert.Equal("Déroulement", timeline.Find("section").GetAttribute("aria-label"));
        Assert.Equal(["0s", "4m00"], timeline.FindAll(".omni-step-timeline__scale span").Select(span => span.TextContent));

        var build = rows[1];
        Assert.Contains("omni-step-timeline__step--failed", build.ClassList);
        Assert.Equal("25%", build.QuerySelector(".omni-step-timeline__bar")!.GetAttribute("x"));
        Assert.Equal("50%", build.QuerySelector(".omni-step-timeline__bar")!.GetAttribute("width"));
        Assert.Equal("2m00", build.QuerySelector(".omni-step-timeline__duration")!.TextContent);
        Assert.Equal("en échec, démarrée à 1m00, durée", build.QuerySelector(".omni-visually-hidden")!.TextContent);

        Assert.Contains("omni-step-timeline__step--secondary", rows[0].ClassList);
        Assert.Contains("omni-step-timeline__step--running", rows[2].ClassList);
        Assert.Contains("Confirm, Commit", timeline.Find(".omni-step-timeline__never").TextContent, StringComparison.Ordinal);
        Assert.Equal("Jamais démarrées :", timeline.Find(".omni-step-timeline__never-label").TextContent);
        Assert.DoesNotContain("style=", timeline.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_WithNothingStarted_ListsTheStepsWithoutAnAxis()
    {
        var timeline = Render<OmniStepTimeline>(parameters => parameters
            .Add(component => component.Steps, [new OmniStepTimelineStep("Seule", null, null)])
            .Add(component => component.Label, "Exécution 12"));

        Assert.Empty(timeline.FindAll(".omni-step-timeline__scale"));
        Assert.Contains("Seule", timeline.Find(".omni-step-timeline__never").TextContent, StringComparison.Ordinal);
        Assert.Equal("Exécution 12", timeline.Find("section").GetAttribute("aria-label"));
    }
}
