using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The size of a one-line field (Astraia recette R-052): a small drop-down sits on the line of small
/// buttons in a toolbar. The class lands on the field element of every one-line control; the
/// stylesheet gives it the height of the button of the same size.
/// </summary>
public sealed class InputSizeTests : OmniBunitContext
{
    private static readonly IReadOnlyList<OmniOption<int>> Options = [new OmniOption<int>(0, "Normal"), new OmniOption<int>(1, "Build")];

    [Theory]
    [InlineData(OmniControlSize.Small, "omni-input--small")]
    [InlineData(OmniControlSize.Large, "omni-input--large")]
    public void EveryOneLineField_CarriesItsSizeOnTheFieldElement(OmniControlSize size, string sizeClass)
    {
        var value = 0;
        var text = "";
        DateOnly? day = null;
        TimeOnly? time = null;
        var fields = new[]
        {
            Render<OmniDropDown<int>>(parameters => parameters.Add(c => c.Options, Options).Add(c => c.Value, value).Add(c => c.ValueExpression, () => value).Add(c => c.Size, size)).Find("select"),
            Render<OmniTextBox>(parameters => parameters.Add(c => c.Value, text).Add(c => c.ValueExpression, () => text).Add(c => c.Size, size)).Find("input"),
            Render<OmniPassword>(parameters => parameters.Add(c => c.Value, text).Add(c => c.ValueExpression, () => text).Add(c => c.Size, size)).Find("input"),
            Render<OmniNumeric<int>>(parameters => parameters.Add(c => c.Value, value).Add(c => c.ValueExpression, () => value).Add(c => c.Size, size)).Find("input"),
            Render<OmniDatePicker>(parameters => parameters.Add(c => c.Value, day).Add(c => c.ValueExpression, () => day).Add(c => c.Size, size)).Find("input"),
            Render<OmniTimePicker>(parameters => parameters.Add(c => c.Value, time).Add(c => c.ValueExpression, () => time).Add(c => c.Size, size)).Find("input")
        };

        Assert.All(fields, field => Assert.Contains(sizeClass, field.ClassList));
    }

    [Fact]
    public void WithoutSize_AFieldKeepsTheControlHeight_AndTheStylesheetMatchesTheButtons()
    {
        var value = 0;
        var select = Render<OmniDropDown<int>>(parameters => parameters.Add(c => c.Options, Options).Add(c => c.Value, value).Add(c => c.ValueExpression, () => value)).Find("select");

        Assert.DoesNotContain(select.ClassList, name => name.StartsWith("omni-input--", StringComparison.Ordinal));
        var css = StylesheetSource.Read();
        Assert.Contains("select.omni-input.omni-input--small:not([multiple]):not([size]) { block-size: calc(var(--omni-control-height) - 0.5rem);", css, StringComparison.Ordinal);
        Assert.Contains("--omni-button-size: calc(var(--omni-control-height) - 0.5rem);", css, StringComparison.Ordinal);
    }
}
