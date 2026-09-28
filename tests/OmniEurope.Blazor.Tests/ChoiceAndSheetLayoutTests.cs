namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Layout defects seen in the published showcase: the HTML editor page, whose fixed width widened the
/// column holding it, its status bar, its toolbar separators, and the button-based choices whose wrapped
/// label was centred and whose box started later than the two-state check box.
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

    [Fact]
    public void ToolbarSeparator_IsDrawnInTheGapBeforeItsGroup_AndClippedAtTheStartOfARow()
    {
        var group = ShippedLookTests.Body(".omni-html-editor__group");
        Assert.Equal("relative", ShippedLookTests.Value(group, "position"));
        Assert.Equal("wrap", ShippedLookTests.Value(group, "flex-wrap"));

        var separator = ShippedLookTests.Body(".omni-html-editor__separator");
        Assert.Equal("absolute", ShippedLookTests.Value(separator, "position"));
        // Centred in the column gap between groups, four paddings and a rule wide.
        Assert.Equal("calc(-2 * var(--omni-space-xs) - var(--omni-border-width))", ShippedLookTests.Value(separator, "inset-inline-start"));

        var toolbar = ShippedLookTests.Body(".omni-html-editor__toolbar");
        Assert.Equal("calc(var(--omni-space-xs) * 4 + var(--omni-border-width))", ShippedLookTests.Value(toolbar, "column-gap"));
        Assert.Equal("hidden", ShippedLookTests.Value(toolbar, "overflow"));
    }

    [Theory]
    [InlineData(".omni-checkbox-nullable")]
    [InlineData(".omni-switch")]
    public void ButtonChoices_StartTheirLabelAtTheBox(string selector)
    {
        Assert.Equal("start", ShippedLookTests.Value(ShippedLookTests.Body(selector), "text-align"));
    }

    [Theory]
    [InlineData(".omni-checkbox-nullable")]
    [InlineData(".omni-switch")]
    public void ButtonChoices_GiveTheirPaddingBack_SoTheirBoxStartsWithTheTwoStateCheckBox(string selector)
    {
        // The padding keeps the focus ring off the box; a negative inline margin of the same length
        // puts the box where a two-state check box stacked with it starts.
        var body = ShippedLookTests.Body(selector);
        Assert.Equal("var(--omni-space-xs)", ShippedLookTests.Value(body, "padding"));
        Assert.Equal("calc(-1 * var(--omni-space-xs))", ShippedLookTests.Value(body, "margin-inline"));
    }
}
