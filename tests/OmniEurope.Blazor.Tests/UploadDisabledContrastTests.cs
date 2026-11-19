namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A disabled upload keeps its text readable (an accessibility audit of colour contrast passes on it): the zone
/// and the field are no longer faded as a whole, which took their text below 4.5:1, but drawn on the muted
/// surface with the muted text, a pair every theme keeps at 4.5:1 (<see cref="ThemeContrastMatrixTests"/>). Only
/// the decorative disc fades. The hint, readable even while the control is disabled, keeps its colour.
/// </summary>
public sealed class UploadDisabledContrastTests
{
    [Fact]
    public void TheDisabledZoneAndField_FadeNoText()
    {
        var faded = ShippedLookTests.Rules()
            .Where(rule => rule.Selector.Contains(".omni-upload--disabled", StringComparison.Ordinal)
                && rule.Body.Contains("opacity", StringComparison.Ordinal))
            .Select(rule => rule.Selector)
            .ToArray();

        Assert.Equal([".omni-upload--disabled .omni-upload__disc"], faded);
    }

    [Fact]
    public void TheDisabledZone_IsTheMutedSurface_AndItsTextTheMutedText()
    {
        Assert.Equal("var(--omni-color-surface-muted)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-upload--disabled .omni-upload__zone"), "background"));
        Assert.Equal("var(--omni-color-text-muted)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-upload--disabled :is(.omni-upload__prompt, .omni-upload__browse)"), "color"));
        Assert.Equal("var(--omni-color-text-muted)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-upload__hint"), "color"));
    }

    [Fact]
    public void TheDisabledField_AndItsButton_AreTheMutedSurface_AndTheMutedText()
    {
        var body = ShippedLookTests.Body(".omni-upload--disabled .omni-upload__field > :is(.omni-upload__field-value, .omni-upload__field-button)");
        Assert.Equal("var(--omni-color-surface-muted)", ShippedLookTests.Value(body, "background"));
        Assert.Equal("var(--omni-color-text-muted)", ShippedLookTests.Value(body, "color"));
    }

    [Fact]
    public void TheDisabledFieldRule_ComesAfterTheHoverOfTheField_SoAPointerOverItChangesNothing()
    {
        var selectors = ShippedLookTests.Rules().Select(rule => rule.Selector).ToList();
        var hover = selectors.IndexOf(".omni-upload__field:hover > .omni-upload__field-button");
        var disabled = selectors.IndexOf(".omni-upload--disabled .omni-upload__field > :is(.omni-upload__field-value, .omni-upload__field-button)");

        Assert.True(hover >= 0 && disabled > hover, $"hover rule at {hover}, disabled rule at {disabled}");
    }
}
