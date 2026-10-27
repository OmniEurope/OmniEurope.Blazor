namespace OmniEurope.Blazor.Showcase.Examples.Forms;

/// <summary>Every check box and switch in every state, each cell with a value of its own.</summary>
public partial class ToggleStatesExample
{
    private bool CheckTrue { get; set; } = true;

    private bool CheckFalse { get; set; }

    private bool CheckDisabled { get; set; } = true;

    private bool? NullableCheckTrue { get; set; } = true;

    private bool? NullableCheckFalse { get; set; } = false;

    private bool? NullableCheckNull { get; set; }

    private bool? NullableCheckDisabled { get; set; } = true;

    private bool? NullableCheckDisabledNull { get; set; }

    private bool SwitchTrue { get; set; } = true;

    private bool SwitchFalse { get; set; }

    private bool SwitchDisabled { get; set; } = true;

    private bool? NullableSwitchTrue { get; set; } = true;

    private bool? NullableSwitchFalse { get; set; } = false;

    private bool? NullableSwitchNull { get; set; }

    private bool? NullableSwitchDisabled { get; set; } = true;

    private bool? NullableSwitchDisabledNull { get; set; }
}
