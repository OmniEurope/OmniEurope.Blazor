using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The request of a client application of 2026-11-17: a field without a label keeps the line of its label,
/// so a button placed in it lines up with the controls of the labelled fields beside it (17 px higher
/// before). The layout itself is measured in the browser; these tests hold the markup and the rule.
/// </summary>
public sealed class FormFieldEmptyLabelTests : OmniBunitContext
{
    [Fact]
    public void WithoutLabel_TheFieldKeepsAnEmptyLabelAsItsFirstChild()
    {
        var field = Render<OmniFormField>(parameters => parameters
            .Add(component => component.Id, "import-field")
            .AddChildContent("<button type=\"button\">Importer</button>"));

        var label = field.Find(".omni-form-field").Children[0];
        Assert.Equal("LABEL", label.TagName);
        Assert.Contains("omni-label", label.ClassList);
        // Nothing to read: no text, no required mark.
        Assert.Equal(string.Empty, label.TextContent.Trim());
        Assert.Empty(label.Children);
    }

    [Fact]
    public void TheLabelOfAField_KeepsOneLineEvenEmpty()
    {
        var styles = StylesheetSource.Read();

        Assert.Contains(".omni-form-field > .omni-label { min-block-size: 1lh; }", styles, StringComparison.Ordinal);
    }
}
