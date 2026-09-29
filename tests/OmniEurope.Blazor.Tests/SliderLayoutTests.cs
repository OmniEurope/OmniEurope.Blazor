namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A range input has an intrinsic width (about 130 px in Chromium) that a flex item keeps as its
/// minimum: in a narrower slot (the volume of a player bar), the slider overflowed onto its
/// neighbours. The input must be allowed to shrink with the slider.
/// </summary>
public sealed class SliderLayoutTests
{
    [Fact]
    public void SliderInput_CanShrinkBelowItsIntrinsicWidth()
    {
        var css = StylesheetSource.Read();
        var rule = css.Split('\n').Single(line => line.StartsWith(".omni-slider__input {", StringComparison.Ordinal));

        Assert.Contains("min-inline-size: 0", rule, StringComparison.Ordinal);
    }
}
