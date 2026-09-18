using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// What the lot 9 added to <see cref="OmniStepTimeline"/>: the columns of values beside the durations
/// (<see cref="OmniStepTimelineColumn"/>) and the fill of a running step against its usual duration
/// (<see cref="OmniStepTimelineStep.ExpectedDuration"/>).
/// </summary>
public sealed class StepTimelineColumnsTests : OmniBunitContext
{
    private static readonly DateTimeOffset Run = new(2026, 3, 2, 9, 0, 0, TimeSpan.Zero);

    private static readonly IReadOnlyList<OmniStepTimelineStep> Steps =
    [
        new("Build", Run, Run.AddSeconds(120), OmniStepTimelineStatus.Success) { ExpectedDuration = TimeSpan.FromSeconds(100) },
        new("Test", Run.AddSeconds(120), null, OmniStepTimelineStatus.Running) { ExpectedDuration = TimeSpan.FromSeconds(120) }
    ];

    [Fact]
    public void Progress_OnlyForARunningStepWithAPositiveUsualDuration()
    {
        StepTimelineBar Bar(OmniStepTimelineStatus status, TimeSpan? expected, double seconds) =>
            new(new OmniStepTimelineStep("S", Run, null, status) { ExpectedDuration = expected }, 0, 100, TimeSpan.Zero, TimeSpan.FromSeconds(seconds));

        Assert.Null(StepTimelineLayout.ProgressOf(Bar(OmniStepTimelineStatus.Success, TimeSpan.FromSeconds(10), 5)));
        Assert.Null(StepTimelineLayout.ProgressOf(Bar(OmniStepTimelineStatus.Running, null, 5)));
        Assert.Null(StepTimelineLayout.ProgressOf(Bar(OmniStepTimelineStatus.Running, TimeSpan.Zero, 5)));
        Assert.Equal(new StepTimelineProgress(0.5, 0), StepTimelineLayout.ProgressOf(Bar(OmniStepTimelineStatus.Running, TimeSpan.FromSeconds(10), 5)));
        Assert.Equal(new StepTimelineProgress(1, 0.5), StepTimelineLayout.ProgressOf(Bar(OmniStepTimelineStatus.Running, TimeSpan.FromSeconds(10), 15)));
        Assert.Equal(new StepTimelineProgress(1, 1), StepTimelineLayout.ProgressOf(Bar(OmniStepTimelineStatus.Running, TimeSpan.FromSeconds(10), 45)));
    }

    [Fact]
    public void RunningStep_FillsItsBarUpToItsUsualDuration_AndSaysHowFar()
    {
        var timeline = Render<OmniStepTimeline>(parameters => parameters
            .Add(component => component.Steps, Steps)
            .Add(component => component.Now, Run.AddSeconds(180)));

        var rows = timeline.FindAll(".omni-step-timeline__step");
        Assert.Empty(rows[0].QuerySelectorAll(".omni-step-timeline__progress"));

        // Test ran 60 s of its usual 120 s: half of its bar, which spans 180-120 = 60 s of 180 s.
        var progress = rows[1].QuerySelector(".omni-step-timeline__progress")!;
        Assert.Equal("66.667%", progress.GetAttribute("x"));
        Assert.Equal("16.667%", progress.GetAttribute("width"));
        Assert.Null(rows[1].QuerySelector(".omni-step-timeline__overrun"));
        Assert.Contains("50 % de la durée habituelle écoulés", rows[1].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void RunningStep_PastItsUsualDuration_DrawsTheOverrunAndSaysSo()
    {
        var timeline = Render<OmniStepTimeline>(parameters => parameters
            .Add(component => component.Steps, [new OmniStepTimelineStep("Deploy", Run, null, OmniStepTimelineStatus.Running) { ExpectedDuration = TimeSpan.FromSeconds(40) }])
            .Add(component => component.Now, Run.AddSeconds(60)));

        var row = timeline.Find(".omni-step-timeline__step");
        Assert.Equal("100%", row.QuerySelector(".omni-step-timeline__progress")!.GetAttribute("width"));
        Assert.Equal("50%", row.QuerySelector(".omni-step-timeline__overrun")!.GetAttribute("width"));
        Assert.Contains("au-delà de la durée habituelle", row.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("style=", timeline.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Columns_AreHeadedOnce_AndEachValueIsHeardWithItsTitle()
    {
        var timeline = Render<OmniStepTimeline>(parameters => parameters
            .Add(component => component.Steps, Steps)
            .Add(component => component.Now, Run.AddSeconds(180))
            .Add(component => component.Columns,
            [
                new OmniStepTimelineColumn("Habituelle", step => step.ExpectedDuration is { } expected ? StepTimelineLayout.Format(expected) : null) { Description = "Durée médiane des dernières exécutions" },
                new OmniStepTimelineColumn("Agent", step => step.Name == "Build" ? "agent-1" : null),
                new OmniStepTimelineColumn("Vide", _ => null)
            ]));

        Assert.Contains("omni-step-timeline--columns", timeline.Find("section").ClassList);
        var head = timeline.Find(".omni-step-timeline__head");
        Assert.Equal("true", head.GetAttribute("aria-hidden"));
        var titles = head.QuerySelectorAll(".omni-step-timeline__column");
        Assert.Equal(["Habituelle", "Agent"], titles.Select(title => title.TextContent));
        Assert.Equal("Durée médiane des dernières exécutions", titles[0].GetAttribute("title"));
        Assert.Null(titles[1].GetAttribute("title"));

        var rows = timeline.FindAll(".omni-step-timeline__step");
        var build = rows[0].QuerySelectorAll(".omni-step-timeline__column");
        Assert.Equal(2, build.Length);
        Assert.Equal("1m40", build[0].QuerySelector("[aria-hidden=true]")!.TextContent);
        Assert.Equal("Habituelle : 1m40", build[0].QuerySelector(".omni-visually-hidden")!.TextContent);
        Assert.Equal("Agent : agent-1", build[1].QuerySelector(".omni-visually-hidden")!.TextContent);

        // A blank cell keeps its place in the row, so the next column stays aligned, and says nothing.
        var test = rows[1].QuerySelectorAll(".omni-step-timeline__column");
        Assert.Equal(2, test.Length);
        Assert.Empty(test[1].TextContent.Trim());
        Assert.Null(test[1].QuerySelector(".omni-visually-hidden"));
    }

    [Fact]
    public void WithoutColumns_TheRowsAreDrawnAsBefore()
    {
        var timeline = Render<OmniStepTimeline>(parameters => parameters
            .Add(component => component.Steps, Steps)
            .Add(component => component.Now, Run.AddSeconds(180))
            .Add(component => component.Columns, [new OmniStepTimelineColumn("Vide", _ => string.Empty)]));

        Assert.DoesNotContain("omni-step-timeline--columns", timeline.Find("section").ClassList);
        Assert.Empty(timeline.FindAll(".omni-step-timeline__columns"));
    }

    [Theory]
    [InlineData(OmniStepTimelineStatus.Success)]
    [InlineData(OmniStepTimelineStatus.Running)]
    [InlineData(OmniStepTimelineStatus.Failed)]
    [InlineData(OmniStepTimelineStatus.Skipped)]
    [InlineData(OmniStepTimelineStatus.Cancelled)]
    public void EveryStatus_HasItsClass(OmniStepTimelineStatus status)
    {
        var timeline = Render<OmniStepTimeline>(parameters => parameters
            .Add(component => component.Steps, [new OmniStepTimelineStep("S", Run, Run.AddSeconds(10), status)])
            .Add(component => component.Now, Run.AddSeconds(10)));

        Assert.Contains($"omni-step-timeline__step--{status.ToString().ToLowerInvariant()}", timeline.Find(".omni-step-timeline__step").ClassList);
        Assert.Equal(5, Enum.GetValues<OmniStepTimelineStatus>().Length);
    }
}
