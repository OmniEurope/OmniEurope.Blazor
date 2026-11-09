using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The closed face of a styled select (appearance: base-select): OmniDropDown renders a button holding a
/// <c>selectedcontent</c>, so the chosen text has a box of its own that ends with an ellipsis. Without it the
/// text of a long option ("Lexend (default)" at the largest text size) wrapped onto a second line and spilled
/// out of the field.
/// </summary>
public sealed class DropDownFaceTests : OmniBunitContext
{
    private static readonly IReadOnlyList<OmniOption<int>> Fonts =
        [new OmniOption<int>(0, "Lexend (default)"), new OmniOption<int>(1, "Inter")];

    [Fact]
    public void Select_draws_its_chosen_text_in_a_face_before_the_options()
    {
        var value = 0;
        var dropDown = Render<OmniDropDown<int>>(parameters => parameters
            .Add(component => component.Options, Fonts)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        var select = dropDown.Find("select");
        var face = select.FirstElementChild!;
        Assert.Equal("BUTTON", face.TagName);
        Assert.Equal("button", face.GetAttribute("type"));
        Assert.Contains("omni-drop-down__face", face.ClassList);
        Assert.Single(face.Children, child => child.LocalName == "selectedcontent");
        Assert.Equal(2, select.QuerySelectorAll("option").Length);
    }

    [Fact]
    public void Face_text_keeps_one_line_and_ends_with_an_ellipsis()
    {
        var face = ShippedLookTests.Body("select.omni-input > .omni-drop-down__face");
        Assert.StartsWith("all: unset", face.Trim(), StringComparison.Ordinal);
        Assert.Equal("0", ShippedLookTests.Value(face, "min-inline-size"));

        var text = ShippedLookTests.Body("select.omni-input > .omni-drop-down__face > selectedcontent");
        Assert.Equal("nowrap", ShippedLookTests.Value(text, "white-space"));
        Assert.Equal("ellipsis", ShippedLookTests.Value(text, "text-overflow"));
        Assert.Equal("hidden", ShippedLookTests.Value(text, "overflow"));
    }
}
