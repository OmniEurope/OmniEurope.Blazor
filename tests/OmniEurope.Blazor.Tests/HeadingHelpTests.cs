using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The help of a heading: a "?" after the heading, outside it, drawn and behaving as the help of a form field.
/// Without a help text the heading is rendered exactly as before.
/// </summary>
public sealed class HeadingHelpTests : OmniBunitContext
{
    [Fact]
    public void WithoutHelp_TheHeadingIsRenderedAlone_AndThereIsNoMark()
    {
        var heading = Render<OmniHeading>(parameters => parameters
            .Add(component => component.Id, "smtp")
            .Add(component => component.Level, OmniHeadingLevel.H3)
            .AddChildContent("Messagerie"));

        Assert.Equal("H3", heading.Nodes.OfType<AngleSharp.Dom.IElement>().Single().TagName);
        Assert.Empty(heading.FindAll(".omni-heading-row"));
        Assert.Empty(heading.FindAll(".omni-tooltip"));
    }

    [Fact]
    public void WithHelp_AFocusableMarkBesideTheHeading_CarriesTheText()
    {
        var heading = Render<OmniHeading>(parameters => parameters
            .Add(component => component.Id, "smtp")
            .Add(component => component.Level, OmniHeadingLevel.H3)
            .Add(component => component.Class, "host-class")
            .Add(component => component.Help, "Serveur qui envoie les notifications.")
            .AddChildContent("Messagerie"));

        var row = heading.Find(".omni-heading-row");
        var title = row.QuerySelector("h3")!;
        Assert.Equal("smtp", title.GetAttribute("id"));
        Assert.Contains("omni-heading--h3", title.ClassList);
        Assert.Contains("host-class", title.ClassList);
        // The mark is beside the heading, never inside it: its text is not a part of the heading's name.
        Assert.Equal("Messagerie", title.TextContent.Trim());
        Assert.Null(title.QuerySelector(".omni-tooltip"));
        var tooltip = row.QuerySelector(".omni-tooltip.omni-heading__help")!;
        Assert.Equal("smtp-help", tooltip.GetAttribute("id"));
        var trigger = tooltip.QuerySelector(".omni-tooltip__trigger")!;
        Assert.Equal("0", trigger.GetAttribute("tabindex"));
        Assert.Equal("smtp-help-content", trigger.GetAttribute("aria-describedby"));
        var mark = trigger.QuerySelector(".omni-heading__help-mark")!;
        Assert.Equal("img", mark.GetAttribute("role"));
        Assert.Equal("Aide", mark.GetAttribute("aria-label"));
        var content = heading.Find("#smtp-help-content");
        Assert.Equal("tooltip", content.GetAttribute("role"));
        Assert.Equal("Serveur qui envoie les notifications.", content.TextContent);
    }

    [Theory]
    [InlineData(OmniHeadingLevel.H1, "H1")]
    [InlineData(OmniHeadingLevel.H2, "H2")]
    [InlineData(OmniHeadingLevel.H4, "H4")]
    [InlineData(OmniHeadingLevel.H5, "H5")]
    [InlineData(OmniHeadingLevel.H6, "H6")]
    public void WithHelp_EveryLevelKeepsItsElement(OmniHeadingLevel level, string tag)
    {
        var heading = Render<OmniHeading>(parameters => parameters
            .Add(component => component.Level, level)
            .Add(component => component.Help, "Aide")
            .AddChildContent("Titre"));

        Assert.Equal(tag, heading.Find(".omni-heading-row > .omni-heading").TagName);
        Assert.Contains($"omni-heading-row--{tag.ToLowerInvariant()}", heading.Find(".omni-heading-row").ClassList);
        Assert.Null(heading.Find(".omni-heading__help").GetAttribute("id"));
    }
}
