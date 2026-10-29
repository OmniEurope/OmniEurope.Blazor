using Bunit;
using OmniEurope.Blazor.Components;
using static OmniEurope.Blazor.Tests.ShippedLookTests;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniProgressBar.Segments"/>: several values stacked on one total. The markup is rendered (width
/// steps, tones, announced values, legend, validation); the look is read from the stylesheet.
/// </summary>
public sealed class ProgressSegmentsTests : OmniBunitContext
{
    private static readonly OmniProgressSegment[] Lesson =
    [
        new(12, "Discovered", OmniTone.Info),
        new(8, "Known", OmniTone.Accent),
        new(3, "Mastered", OmniTone.Success)
    ];

    private IRenderedComponent<OmniProgressBar> RenderLesson(Action<ComponentParameterCollectionBuilder<OmniProgressBar>>? more = null) =>
        Render<OmniProgressBar>(parameters =>
        {
            parameters
                .Add(component => component.Label, "Lesson progress")
                .Add(component => component.Maximum, 30)
                .Add(component => component.Segments, Lesson);
            more?.Invoke(parameters);
        });

    [Fact]
    public void Segments_AreDrawnEndToEndInTheirTonesAtTheRoundedBoundaries()
    {
        var bar = RenderLesson();

        var segments = bar.FindAll(".omni-progress__track .omni-progress__segment");
        Assert.Equal(3, segments.Count);
        // Boundaries at 40 %, 66.7 % and 76.7 % of the total, each rounded once to a step of 5: 40, 65, 75.
        Assert.Equal(
            ["omni-progress__indicator--40", "omni-progress__indicator--25", "omni-progress__indicator--10"],
            segments.Select(segment => segment.ClassList.Single(name => name.StartsWith("omni-progress__indicator--", StringComparison.Ordinal))));
        Assert.Equal(
            ["omni-progress__segment--info", "omni-progress__segment--accent", "omni-progress__segment--success"],
            segments.Select(segment => segment.ClassList.Single(name => name.StartsWith("omni-progress__segment--", StringComparison.Ordinal))));
        Assert.Equal(["Discovered 12", "Known 8", "Mastered 3"], segments.Select(segment => segment.GetAttribute("title")));
        Assert.Contains("omni-progress--segments", bar.Find(".omni-progress").ClassList);
    }

    [Fact]
    public void TheBar_AnnouncesTheSumAndEachSegmentAndKeepsTheProgressbarRole()
    {
        var bar = RenderLesson().Find("[role=progressbar]");

        Assert.Equal("Lesson progress", bar.GetAttribute("aria-label"));
        Assert.Equal("0", bar.GetAttribute("aria-valuemin"));
        Assert.Equal("30", bar.GetAttribute("aria-valuemax"));
        Assert.Equal("23", bar.GetAttribute("aria-valuenow"));
        Assert.Equal("Discovered 12, Known 8, Mastered 3", bar.GetAttribute("aria-valuetext"));
    }

    [Fact]
    public void ValueText_ReplacesTheSegmentSummaryAndShowValueWritesThePercentageOfTheSum()
    {
        var withText = RenderLesson(parameters => parameters.Add(component => component.ValueText, "23 of 30 words")).Find("[role=progressbar]");
        Assert.Equal("23 of 30 words", withText.GetAttribute("aria-valuetext"));

        var shown = RenderLesson(parameters => parameters.Add(component => component.ShowValue, true));
        Assert.Equal("77%", shown.Find(".omni-progress__label").TextContent.Replace(" ", string.Empty, StringComparison.Ordinal));
    }

    [Fact]
    public void Segments_NeverPassTheMaximum_AndNegativeAmountsCountAsZero()
    {
        var bar = Render<OmniProgressBar>(parameters => parameters
            .Add(component => component.Maximum, 10)
            .Add(component => component.Segments, new OmniProgressSegment[] { new(-4, "Ignored"), new(8, "First"), new(8, "Second") }));

        var announced = bar.Find("[role=progressbar]");
        Assert.Equal("10", announced.GetAttribute("aria-valuenow"));
        Assert.Equal("Ignored 0, First 8, Second 2", announced.GetAttribute("aria-valuetext"));
        Assert.Equal(
            ["omni-progress__indicator--80", "omni-progress__indicator--20"],
            bar.FindAll(".omni-progress__segment").Select(segment => segment.ClassList.Single(name => name.StartsWith("omni-progress__indicator--", StringComparison.Ordinal))));
    }

    [Fact]
    public void ASegmentUnderHalfAStepIsNotDrawnButStaysAnnounced()
    {
        var bar = Render<OmniProgressBar>(parameters => parameters
            .Add(component => component.Segments, new OmniProgressSegment[] { new(1, "Tiny"), new(50, "Half") }));

        Assert.Single(bar.FindAll(".omni-progress__segment"));
        Assert.Equal("Tiny 1, Half 50", bar.Find("[role=progressbar]").GetAttribute("aria-valuetext"));
    }

    [Fact]
    public void Legend_ListsEverySegmentWithItsToneOnlyWhenAsked()
    {
        Assert.Empty(RenderLesson().FindAll(".omni-progress__legend"));

        var bar = RenderLesson(parameters => parameters.Add(component => component.ShowLegend, true));

        var items = bar.FindAll(".omni-progress__legend-item");
        Assert.Equal(["Discovered 12", "Known 8", "Mastered 3"], items.Select(item => item.TextContent));
        Assert.Contains("omni-progress__segment--success", items[2].QuerySelector(".omni-progress__swatch")!.ClassList);
        Assert.Contains("omni-progress--legend", bar.Find(".omni-progress").ClassList);
    }

    [Fact]
    public void WithoutSegments_TheSingleValueBarIsUnchanged()
    {
        var bar = Render<OmniProgressBar>(parameters => parameters
            .Add(component => component.Value, 40)
            .Add(component => component.ShowLegend, true));

        var root = bar.Find(".omni-progress");
        Assert.DoesNotContain("omni-progress--segments", root.ClassList);
        Assert.DoesNotContain("omni-progress--legend", root.ClassList);
        Assert.Empty(bar.FindAll(".omni-progress__segment"));
        Assert.Empty(bar.FindAll(".omni-progress__legend"));
        Assert.Single(bar.FindAll(".omni-progress__indicator--40"));
        Assert.Equal("40", root.GetAttribute("aria-valuenow"));

        var empty = Render<OmniProgressBar>(parameters => parameters
            .Add(component => component.Value, 40)
            .Add(component => component.Segments, Array.Empty<OmniProgressSegment>()));
        Assert.Single(empty.FindAll(".omni-progress__indicator--40"));
    }

    [Fact]
    public void AnIndeterminateBarDrawsNoSegment()
    {
        var bar = RenderLesson(parameters => parameters.Add(component => component.Indeterminate, true));

        var root = bar.Find("[role=progressbar]");
        Assert.Empty(bar.FindAll(".omni-progress__segment"));
        Assert.False(root.HasAttribute("aria-valuenow"));
        Assert.False(root.HasAttribute("aria-valuetext"));
    }

    [Fact]
    public void ACircularBarDrawsOneArcPerSegmentFromWhereTheLastEnded()
    {
        var bar = RenderLesson(parameters => parameters.Add(component => component.Shape, OmniProgressShape.Circular));

        var arcs = bar.FindAll("circle.omni-progress__circle-value");
        Assert.Equal(3, arcs.Count);
        Assert.Equal(["40 100", "26.67 100", "10 100"], arcs.Select(arc => arc.GetAttribute("stroke-dasharray")));
        Assert.Equal(["0", "-40", "-66.67"], arcs.Select(arc => arc.GetAttribute("stroke-dashoffset")));
        Assert.Equal("Discovered 12", arcs[0].QuerySelector("title")!.TextContent);
        Assert.Contains("omni-progress__segment--accent", arcs[1].ClassList);
    }

    [Fact]
    public void TheMarkupCarriesNoInlineStyle()
    {
        var linear = RenderLesson(parameters => parameters.Add(component => component.ShowLegend, true));
        var circular = RenderLesson(parameters => parameters.Add(component => component.Shape, OmniProgressShape.Circular));

        Assert.Empty(linear.FindAll("[style]"));
        Assert.Empty(circular.FindAll("[style]"));
    }

    [Fact]
    public void ASegmentWithoutALabelOrANumberIsRefused()
    {
        Assert.Throws<ArgumentException>(() => Render<OmniProgressBar>(parameters => parameters
            .Add(component => component.Segments, new OmniProgressSegment[] { new(1, " ") })));
        Assert.Throws<ArgumentException>(() => Render<OmniProgressBar>(parameters => parameters
            .Add(component => component.Segments, new OmniProgressSegment[] { new(double.NaN, "Broken") })));
    }

    [Fact]
    public void EverySegmentToneTakesTheColourOfTheBarOfTheSameTone()
    {
        foreach (var tone in Enum.GetValues<OmniTone>())
        {
            var name = tone.ToString().ToLowerInvariant();
            var bar = tone == OmniTone.Accent ? Body(".omni-progress") : Body($".omni-progress--{name}");
            Assert.Equal(Value(bar, "color"), Value(Body($".omni-progress__segment--{name}"), "color"));
        }
    }
}
