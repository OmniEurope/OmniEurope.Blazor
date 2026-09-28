namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Layout defects seen in the published showcase: the HTML editor page, whose fixed width widened the
/// column holding it, its status bar, and the button-based choices whose wrapped label was centred.
/// </summary>
public sealed class ChoiceAndSheetLayoutTests
{
    [Fact]
    public void Sheet_CapsItsPageAtFiftyRemInsteadOfFixingItsWidth()
    {
        var body = ShippedLookTests.Body(".omni-document-editor--sheet .omni-html-editor__surface");

        Assert.Equal("50rem", ShippedLookTests.Value(body, "max-width"));
        Assert.Equal("calc(100% - 2 * var(--omni-space-md))", ShippedLookTests.Value(body, "width"));
    }

    [Fact]
    public void StatusBar_KeepsTheCountsTogetherAndSendsTheExportsToTheEnd()
    {
        Assert.Equal("flex-start", ShippedLookTests.Value(ShippedLookTests.Body(".omni-document-editor__status"), "justify-content"));
        Assert.Equal("auto", ShippedLookTests.Value(ShippedLookTests.Body(".omni-document-editor__actions"), "margin-inline-start"));
    }

    [Theory]
    [InlineData(".omni-checkbox-nullable")]
    [InlineData(".omni-switch")]
    public void ButtonChoices_StartTheirLabelAtTheBox(string selector)
    {
        Assert.Equal("start", ShippedLookTests.Value(ShippedLookTests.Body(selector), "text-align"));
    }
}
