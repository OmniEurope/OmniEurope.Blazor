using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class FoundationComponentTests : OmniBunitContext
{
    [Theory]
    [InlineData(nameof(OmniText))]
    [InlineData(nameof(OmniHeading))]
    [InlineData(nameof(OmniIcon))]
    [InlineData(nameof(OmniBadge))]
    [InlineData(nameof(OmniLink))]
    [InlineData(nameof(OmniImage))]
    [InlineData(nameof(OmniSkeleton))]
    [InlineData(nameof(OmniRow))]
    [InlineData(nameof(OmniColumn))]
    [InlineData(nameof(OmniGrid))]
    [InlineData(nameof(OmniLayout))]
    [InlineData(nameof(OmniMain))]
    [InlineData(nameof(OmniHeader))]
    [InlineData(nameof(OmniFieldset))]
    [InlineData(nameof(OmniProgressBar))]
    public void FoundationComponent_RendersWithoutInlineStyle(string componentName) =>
        AssertMarkupHasNoInlineStyle(RenderFoundation(componentName));

    [Fact]
    public void Text_RendersRequestedParagraphElement()
    {
        var component = Render<OmniText>(parameters => parameters
            .Add(item => item.Element, OmniTextElement.Paragraph)
            .AddChildContent("Paragraph"));

        Assert.Equal("Paragraph", component.Find("p").TextContent);
    }

    [Fact]
    public void Heading_RendersRequestedLevel()
    {
        var component = Render<OmniHeading>(parameters => parameters
            .Add(item => item.Level, OmniHeadingLevel.H3)
            .AddChildContent("Section"));

        Assert.Equal("Section", component.Find("h3").TextContent);
    }

    [Fact]
    public void Icon_ExposesAccessibleImageSemantics()
    {
        var component = Render<OmniIcon>(parameters => parameters
            .Add(item => item.Name, OmniIconName.Info)
            .Add(item => item.AriaLabel, "Information"));

        Assert.Equal("img", component.Find("svg").GetAttribute("role"));
        Assert.Equal("Information", component.Find("svg").GetAttribute("aria-label"));
    }

    [Fact]
    public void Link_NewTabAddsSafeRelationship()
    {
        var component = Render<OmniLink>(parameters => parameters
            .Add(item => item.Href, "https://example.test")
            .Add(item => item.NewTab, true)
            .AddChildContent("External"));

        Assert.Equal("_blank", component.Find("a").GetAttribute("target"));
        Assert.Equal("noopener noreferrer", component.Find("a").GetAttribute("rel"));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("vbscript:msgbox(1)")]
    public void Link_RejectsActiveUriSchemes(string href)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Render<OmniLink>(parameters => parameters
                .Add(component => component.Href, href)
                .AddChildContent("Unsafe")));

        Assert.Contains("URI scheme", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/relative")]
    [InlineData("https://example.test/path")]
    [InlineData("mailto:test@example.test")]
    [InlineData("tel:+32000000000")]
    public void Link_AllowsSupportedUris(string href)
    {
        var link = Render<OmniLink>(parameters => parameters
            .Add(component => component.Href, href)
            .AddChildContent("Safe"));

        Assert.Equal(href, link.Find("a").GetAttribute("href"));
    }

    [Fact]
    public void Row_RendersGapAndWrapClasses()
    {
        var component = Render<OmniRow>(parameters => parameters
            .Add(item => item.Gap, OmniSpacing.Large)
            .Add(item => item.Wrap, true)
            .AddChildContent("Row"));

        Assert.Contains("omni-row--gap-large", component.Find("div").ClassList);
        Assert.Contains("omni-row--wrap", component.Find("div").ClassList);
    }

    [Fact]
    public void Column_RendersResponsiveSpans()
    {
        var component = Render<OmniColumn>(parameters => parameters
            .Add(item => item.Span, 12)
            .Add(item => item.MediumSpan, 6)
            .AddChildContent("Column"));

        Assert.Contains("omni-column--span-12", component.Find("div").ClassList);
        Assert.Contains("omni-column--md-6", component.Find("div").ClassList);
    }

    [Fact]
    public void Grid_RendersRequestedColumnClass()
    {
        var component = Render<OmniGrid>(parameters => parameters
            .Add(item => item.Columns, 4)
            .AddChildContent("Grid"));

        Assert.Contains("omni-grid--columns-4", component.Find("div").ClassList);
    }

    [Fact]
    public void Main_IsProgrammaticallyFocusable()
    {
        var component = Render<OmniMain>(parameters => parameters
            .Add(item => item.Id, "content")
            .AddChildContent("Main"));

        Assert.Equal("-1", component.Find("main").GetAttribute("tabindex"));
    }

    [Fact]
    public void Header_RendersStickyClass()
    {
        var component = Render<OmniHeader>(parameters => parameters
            .Add(item => item.Sticky, true)
            .AddChildContent("Header"));

        Assert.Contains("omni-header--sticky", component.Find("header").ClassList);
    }

    [Fact]
    public void Skeleton_ExposesLoadingStatusAndLineCount()
    {
        var component = Render<OmniSkeleton>(parameters => parameters
            .Add(item => item.LineCount, 3)
            .Add(item => item.Label, "Loading content"));

        Assert.Equal("status", component.Find("div").GetAttribute("role"));
        Assert.Equal(3, component.FindAll(".omni-skeleton__item").Count);
    }

    [Fact]
    public void Fieldset_RendersLegend()
    {
        var component = Render<OmniFieldset>(parameters => parameters
            .Add(item => item.Legend, Content("Contact"))
            .AddChildContent("Fields"));

        Assert.Equal("Contact", component.Find("legend").TextContent);
    }

    [Fact]
    public void LinearProgress_ExposesValueAndBucketClass()
    {
        var component = Render<OmniProgressBar>(parameters => parameters
            .Add(item => item.Value, 42d)
            .Add(item => item.Label, "Upload")
            .Add(item => item.ShowValue, true));

        Assert.Equal("42", component.Find("[role=progressbar]").GetAttribute("aria-valuenow"));
        Assert.Contains("omni-progress__indicator--40", component.Find(".omni-progress__indicator").ClassList);
    }

    [Fact]
    public void CircularProgress_RendersDashArray()
    {
        var component = Render<OmniProgressBar>(parameters => parameters
            .Add(item => item.Value, 75d)
            .Add(item => item.Label, "Completion")
            .Add(item => item.Shape, OmniProgressShape.Circular));

        Assert.Equal("75 100", component.Find(".omni-progress__circle-value").GetAttribute("stroke-dasharray"));
    }

    [Fact]
    public void Column_RejectsSpanAboveTwelve() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Render<OmniColumn>(parameters => parameters
            .Add(component => component.Span, 13)
            .AddChildContent("Invalid")));

    [Fact]
    public void Grid_RejectsZeroColumns() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Render<OmniGrid>(parameters => parameters
            .Add(component => component.Columns, 0)
            .AddChildContent("Invalid")));

    [Fact]
    public void Progress_RejectsEqualBounds() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Render<OmniProgressBar>(parameters => parameters
            .Add(component => component.Minimum, 10d)
            .Add(component => component.Maximum, 10d)));

    private string RenderFoundation(string componentName) => componentName switch
    {
        nameof(OmniText) => Render<OmniText>(parameters => parameters.AddChildContent("Text")).Markup,
        nameof(OmniHeading) => Render<OmniHeading>(parameters => parameters.AddChildContent("Heading")).Markup,
        nameof(OmniIcon) => Render<OmniIcon>().Markup,
        nameof(OmniBadge) => Render<OmniBadge>(parameters => parameters.AddChildContent("Badge")).Markup,
        nameof(OmniLink) => Render<OmniLink>(parameters => parameters.Add(item => item.Href, "/target").AddChildContent("Link")).Markup,
        nameof(OmniImage) => Render<OmniImage>(parameters => parameters.Add(item => item.Source, "/image.png").Add(item => item.Alt, "Description")).Markup,
        nameof(OmniSkeleton) => Render<OmniSkeleton>().Markup,
        nameof(OmniRow) => Render<OmniRow>(parameters => parameters.AddChildContent("Row")).Markup,
        nameof(OmniColumn) => Render<OmniColumn>(parameters => parameters.AddChildContent("Column")).Markup,
        nameof(OmniGrid) => Render<OmniGrid>(parameters => parameters.AddChildContent("Grid")).Markup,
        nameof(OmniLayout) => Render<OmniLayout>(parameters => parameters.AddChildContent("Layout")).Markup,
        nameof(OmniMain) => Render<OmniMain>(parameters => parameters.AddChildContent("Main")).Markup,
        nameof(OmniHeader) => Render<OmniHeader>(parameters => parameters.AddChildContent("Header")).Markup,
        nameof(OmniFieldset) => Render<OmniFieldset>(parameters => parameters.Add(item => item.Legend, Content("Legend")).AddChildContent("Fields")).Markup,
        nameof(OmniProgressBar) => Render<OmniProgressBar>().Markup,
        _ => throw new ArgumentOutOfRangeException(nameof(componentName), componentName, null)
    };

    private static RenderFragment Content(string value) => builder => builder.AddContent(0, value);

    private static void AssertMarkupHasNoInlineStyle(string markup) =>
        Assert.False(markup.Contains("style=", StringComparison.OrdinalIgnoreCase), markup);
}
