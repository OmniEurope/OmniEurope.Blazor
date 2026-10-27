using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Forms;

/// <summary>A check box, a switch and their three-state forms, whose settings are parameters.</summary>
public partial class TogglesExample
{
    /// <summary>Whether the controls keep their value but can no longer change it.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether a click on a three-state control also reaches the null state.</summary>
    [Parameter]
    public bool AllowIndeterminate { get; set; } = true;

    /// <summary>Whether each control shows its text; without it the name is only an accessible label.</summary>
    [Parameter]
    public bool ShowLabels { get; set; } = true;

    /// <summary>Whether the text of a check box or a switch goes before the control, as in a settings row.</summary>
    [Parameter]
    public bool TextFirst { get; set; }

    private bool Checked { get; set; } = true;

    private bool? NullableChecked { get; set; }

    private bool Switched { get; set; }

    private bool? NullableSwitched { get; set; }

    /// <summary>Forces the four values at once; null only reaches the two three-state controls.</summary>
    /// <param name="value">The value every control takes.</param>
    public void SetAll(bool value)
    {
        Checked = value;
        NullableChecked = value;
        Switched = value;
        NullableSwitched = value;
        StateHasChanged();
    }

    /// <summary>Puts the two three-state controls back to null, which a click may no longer reach.</summary>
    public void SetAllNull()
    {
        NullableChecked = null;
        NullableSwitched = null;
        StateHasChanged();
    }

    private static string Describe(bool? value) => value switch
    {
        true => "true",
        false => "false",
        null => "null (indeterminate)"
    };
}
