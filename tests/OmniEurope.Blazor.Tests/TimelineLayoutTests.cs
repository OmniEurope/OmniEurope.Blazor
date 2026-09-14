using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The side <see cref="OmniTimeline"/> sets its entries on. The placement itself is drawn by the
/// stylesheet, checked in Chromium; these tests pin the classes it keys on.
/// </summary>
public sealed class TimelineLayoutTests : OmniBunitContext
{
    [Fact]
    public void DefaultLayout_KeepsTheEntriesAfterTheLine()
    {
        var timeline = Render<OmniTimeline>(parameters => parameters
            .AddChildContent<OmniTimelineItem>(item => item.Add(component => component.Title, "Dépôt")));

        var section = timeline.Find("section");
        Assert.Contains("omni-timeline--end", section.ClassList);
        Assert.DoesNotContain(section.ClassList, name => name.StartsWith("omni-timeline--first-", StringComparison.Ordinal));
    }

    [Fact]
    public void StartLayout_SetsTheEntriesBeforeTheLine_AndIgnoresTheFirstSide()
    {
        var timeline = Render<OmniTimeline>(parameters => parameters
            .Add(component => component.Layout, OmniTimelineLayout.Start)
            .Add(component => component.FirstSide, OmniTimelineSide.Start));

        var section = timeline.Find("section");
        Assert.Contains("omni-timeline--start", section.ClassList);
        Assert.DoesNotContain(section.ClassList, name => name.StartsWith("omni-timeline--first-", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(OmniTimelineSide.Start, "omni-timeline--first-start")]
    [InlineData(OmniTimelineSide.End, "omni-timeline--first-end")]
    public void AlternateLayout_NamesTheSideOfTheFirstEntry(OmniTimelineSide side, string expected)
    {
        var timeline = Render<OmniTimeline>(parameters => parameters
            .Add(component => component.Layout, OmniTimelineLayout.Alternate)
            .Add(component => component.FirstSide, side)
            .Add(component => component.Class, "hote"));

        var section = timeline.Find("section");
        Assert.Contains("omni-timeline--alternate", section.ClassList);
        Assert.Contains(expected, section.ClassList);
        Assert.Contains("hote", section.ClassList);
        Assert.DoesNotContain("style=", timeline.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Stylesheet_AlternatesByRankOnlyInsideAWideEnoughTimeline()
    {
        var css = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot", "omnieurope.blazor.css"));

        Assert.Contains(".omni-timeline--alternate { container-type: inline-size; }", css, StringComparison.Ordinal);
        var query = css.IndexOf("@container (min-width: 30rem)", StringComparison.Ordinal);
        Assert.True(query > 0);
        Assert.True(css.IndexOf(".omni-timeline--first-start .omni-timeline__item:nth-child(odd) .omni-timeline__content", StringComparison.Ordinal) > query);
        Assert.True(css.IndexOf(".omni-timeline--first-end .omni-timeline__item:nth-child(even) .omni-timeline__content", StringComparison.Ordinal) > query);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OmniEurope.Blazor.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }
}
