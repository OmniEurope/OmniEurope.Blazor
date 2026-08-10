using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class FoundationComponentTests : BunitContext
{
    [Fact]
    public void FoundationBatch_RendersFifteenComponentsWithoutInlineStyles()
    {
        var markups = new[]
        {
            Render<OmniText>(parameters => parameters.AddChildContent("Text")).Markup,
            Render<OmniHeading>(parameters => parameters.AddChildContent("Heading")).Markup,
            Render<OmniIcon>().Markup,
            Render<OmniBadge>(parameters => parameters.AddChildContent("Badge")).Markup,
            Render<OmniLink>(parameters => parameters.Add(component => component.Href, "/target").AddChildContent("Link")).Markup,
            Render<OmniImage>(parameters => parameters
                .Add(component => component.Source, "/image.png")
                .Add(component => component.Alt, "Description")).Markup,
            Render<OmniSkeleton>().Markup,
            Render<OmniRow>(parameters => parameters.AddChildContent("Row")).Markup,
            Render<OmniColumn>(parameters => parameters.AddChildContent("Column")).Markup,
            Render<OmniGrid>(parameters => parameters.AddChildContent("Grid")).Markup,
            Render<OmniLayout>(parameters => parameters.AddChildContent("Layout")).Markup,
            Render<OmniMain>(parameters => parameters.AddChildContent("Main")).Markup,
            Render<OmniHeader>(parameters => parameters.AddChildContent("Header")).Markup,
            Render<OmniFieldset>(parameters => parameters
                .Add(component => component.Legend, Content("Legend"))
                .AddChildContent("Fields")).Markup,
            Render<OmniProgressBar>().Markup
        };

        Assert.Equal(15, markups.Length);
        Assert.All(markups, AssertMarkupHasNoInlineStyle);
    }

    [Fact]
    public void ContentComponents_RenderRequestedSemantics()
    {
        var text = Render<OmniText>(parameters => parameters
            .Add(component => component.Element, OmniTextElement.Paragraph)
            .AddChildContent("Paragraph"));
        var heading = Render<OmniHeading>(parameters => parameters
            .Add(component => component.Level, OmniHeadingLevel.H3)
            .AddChildContent("Section"));
        var icon = Render<OmniIcon>(parameters => parameters
            .Add(component => component.Name, OmniIconName.Info)
            .Add(component => component.AriaLabel, "Information"));
        var link = Render<OmniLink>(parameters => parameters
            .Add(component => component.Href, "https://example.test")
            .Add(component => component.NewTab, true)
            .AddChildContent("External"));

        Assert.Equal("Paragraph", text.Find("p").TextContent);
        Assert.Equal("Section", heading.Find("h3").TextContent);
        Assert.Equal("img", icon.Find("svg").GetAttribute("role"));
        Assert.Equal("Information", icon.Find("svg").GetAttribute("aria-label"));
        Assert.Equal("_blank", link.Find("a").GetAttribute("target"));
        Assert.Equal("noopener noreferrer", link.Find("a").GetAttribute("rel"));
    }

    [Fact]
    public void LayoutComponents_RenderResponsiveClassesAndLandmarks()
    {
        var row = Render<OmniRow>(parameters => parameters
            .Add(component => component.Gap, OmniSpacing.Large)
            .Add(component => component.Wrap, true)
            .AddChildContent("Row"));
        var column = Render<OmniColumn>(parameters => parameters
            .Add(component => component.Span, 12)
            .Add(component => component.MediumSpan, 6)
            .AddChildContent("Column"));
        var grid = Render<OmniGrid>(parameters => parameters
            .Add(component => component.Columns, 4)
            .AddChildContent("Grid"));
        var main = Render<OmniMain>(parameters => parameters
            .Add(component => component.Id, "content")
            .AddChildContent("Main"));
        var header = Render<OmniHeader>(parameters => parameters
            .Add(component => component.Sticky, true)
            .AddChildContent("Header"));

        Assert.Contains("omni-row--gap-large", row.Find("div").ClassList);
        Assert.Contains("omni-row--wrap", row.Find("div").ClassList);
        Assert.Contains("omni-column--span-12", column.Find("div").ClassList);
        Assert.Contains("omni-column--md-6", column.Find("div").ClassList);
        Assert.Contains("omni-grid--columns-4", grid.Find("div").ClassList);
        Assert.Equal("-1", main.Find("main").GetAttribute("tabindex"));
        Assert.Contains("omni-header--sticky", header.Find("header").ClassList);
    }

    [Fact]
    public void FeedbackComponents_ExposeAccessibleState()
    {
        var skeleton = Render<OmniSkeleton>(parameters => parameters
            .Add(component => component.LineCount, 3)
            .Add(component => component.Label, "Loading content"));
        var fieldset = Render<OmniFieldset>(parameters => parameters
            .Add(component => component.Legend, Content("Contact"))
            .AddChildContent("Fields"));
        var progress = Render<OmniProgressBar>(parameters => parameters
            .Add(component => component.Value, 42d)
            .Add(component => component.Label, "Upload")
            .Add(component => component.ShowValue, true));
        var circular = Render<OmniProgressBar>(parameters => parameters
            .Add(component => component.Value, 75d)
            .Add(component => component.Label, "Completion")
            .Add(component => component.Shape, OmniProgressShape.Circular));

        Assert.Equal("status", skeleton.Find("div").GetAttribute("role"));
        Assert.Equal(3, skeleton.FindAll(".omni-skeleton__item").Count);
        Assert.Equal("Contact", fieldset.Find("legend").TextContent);
        Assert.Equal("42", progress.Find("[role=progressbar]").GetAttribute("aria-valuenow"));
        Assert.Contains("omni-progress__indicator--40", progress.Find(".omni-progress__indicator").ClassList);
        Assert.Equal("75 100", circular.Find(".omni-progress__circle-value").GetAttribute("stroke-dasharray"));
    }

    [Fact]
    public void InvalidLayoutAndProgressRanges_AreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Render<OmniColumn>(parameters => parameters
                .Add(component => component.Span, 13)
                .AddChildContent("Invalid")));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Render<OmniGrid>(parameters => parameters
                .Add(component => component.Columns, 0)
                .AddChildContent("Invalid")));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Render<OmniProgressBar>(parameters => parameters
                .Add(component => component.Minimum, 10d)
                .Add(component => component.Maximum, 10d)));
    }

    private static RenderFragment Content(string value) => builder => builder.AddContent(0, value);

    private static void AssertMarkupHasNoInlineStyle(string markup) =>
        Assert.False(markup.Contains("style=", StringComparison.OrdinalIgnoreCase), markup);
}
