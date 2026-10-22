using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The one CSS length check behind every height and width a component writes as a custom property:
/// ASCII digits, an optional fraction, then a lowercase unit of the axis, and nothing else.
/// </summary>
public sealed class CssLengthTests
{
    [Theory]
    [InlineData("20rem")]
    [InlineData("320px")]
    [InlineData("50vh")]
    [InlineData("1.5em")]
    [InlineData("100%")]
    [InlineData("0px")]
    [InlineData("0.5rem")]
    [InlineData("007px")]
    [InlineData("123456789.123456px")]
    public void IsHeight_AcceptsANumberAndAHeightUnit(string value)
    {
        Assert.True(CssLength.IsHeight(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("20")]
    [InlineData("rem")]
    [InlineData("%")]
    [InlineData("1.rem")]
    [InlineData(".5rem")]
    [InlineData("1.5")]
    [InlineData("20pt")]
    [InlineData("60vw")]
    [InlineData("60ch")]
    [InlineData("-1px")]
    [InlineData("+1px")]
    [InlineData("1e3px")]
    [InlineData("1.5.5px")]
    [InlineData("20 rem")]
    [InlineData(" 20rem")]
    [InlineData("20rem ")]
    [InlineData("20REM")]
    [InlineData("20rem\n")]
    [InlineData("٣px")]
    [InlineData("20rem;color:red")]
    [InlineData("calc(100vh - 2rem)")]
    [InlineData("var(--x)")]
    [InlineData("auto")]
    [InlineData("none")]
    public void IsHeight_RefusesEverythingElse(string? value)
    {
        Assert.False(CssLength.IsHeight(value));
    }

    [Theory]
    [InlineData("500px")]
    [InlineData("30rem")]
    [InlineData("42.5em")]
    [InlineData("60ch")]
    [InlineData("80vw")]
    [InlineData("90%")]
    [InlineData("0px")]
    public void IsWidth_AcceptsANumberAndAWidthUnit(string value)
    {
        Assert.True(CssLength.IsWidth(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("30")]
    [InlineData("100vh")]
    [InlineData("30REM")]
    [InlineData("30 rem")]
    [InlineData("30rem;color:red")]
    [InlineData("30rem\n")]
    [InlineData("var(--omni-x)")]
    public void IsWidth_RefusesAHeightUnitAndEverythingElse(string? value)
    {
        Assert.False(CssLength.IsWidth(value));
    }

    [Theory]
    [InlineData("0px")]
    [InlineData("0.0rem")]
    [InlineData("000%")]
    public void IsWidth_WhenPositive_RefusesZero(string value)
    {
        Assert.False(CssLength.IsWidth(value, positive: true));
    }

    [Theory]
    [InlineData("1px")]
    [InlineData("0.5rem")]
    [InlineData("0.001ch")]
    [InlineData("10.0vw")]
    public void IsWidth_WhenPositive_AcceptsAnyNumberAboveZero(string value)
    {
        Assert.True(CssLength.IsWidth(value, positive: true));
    }
}
