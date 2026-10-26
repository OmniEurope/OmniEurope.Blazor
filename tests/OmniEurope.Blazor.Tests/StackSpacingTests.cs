using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The request of a client application of 2026-10-26: in an OmniStack, a heading stood 8px farther from the
/// next block than the gap, a paragraph of OmniText 14px (the browser's margin) and a tab panel 16px from its
/// strip, where every other block stands at the gap (12px). The gaps measured in a browser are the scripts
/// probe's (requests); here, the markup the stylesheet rules name and the rules themselves.
/// </summary>
public sealed class StackSpacingTests : OmniBunitContext
{
    [Fact]
    public void AHeadingAndAParagraph_AreDirectChildrenOfTheStack()
    {
        var stack = Render<OmniStack>(parameters => parameters
            .Add(component => component.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniHeading>(0);
                builder.AddComponentParameter(1, nameof(OmniHeading.Level), OmniHeadingLevel.H5);
                builder.AddComponentParameter(2, nameof(OmniHeading.ChildContent), (RenderFragment)(inner => inner.AddContent(0, "Services")));
                builder.CloseComponent();
                builder.OpenComponent<OmniText>(3);
                builder.AddComponentParameter(4, nameof(OmniText.Element), OmniTextElement.Paragraph);
                builder.AddComponentParameter(5, nameof(OmniText.ChildContent), (RenderFragment)(inner => inner.AddContent(0, "Ports")));
                builder.CloseComponent();
            })));

        Assert.Equal("H5", stack.Find(".omni-stack > .omni-heading").TagName);
        Assert.Equal("P", stack.Find(".omni-stack > .omni-text").TagName);
    }

    [Fact]
    public void AStackOrARow_SpacesTheHeadingsAndParagraphsByItsGapAlone()
    {
        var styles = StylesheetSource.Read();

        Assert.Contains(":is(.omni-stack, .omni-row) > :is(.omni-heading, .omni-heading-row, .omni-text) { margin-block: 0; }", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void ATabPanel_StandsFromItsStripByThePageGap_AndAddsNothingBelow()
    {
        var styles = StylesheetSource.Read();

        Assert.Matches(@"\.omni-tabs__panel \{[^}]*padding: var\(--omni-space-md\) 0 0;", styles);
        Assert.Contains(".omni-stack--gap-md { gap: var(--omni-space-md); }", styles, StringComparison.Ordinal);
    }
}
