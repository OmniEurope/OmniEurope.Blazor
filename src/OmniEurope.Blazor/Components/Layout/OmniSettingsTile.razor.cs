namespace OmniEurope.Blazor.Components;

public partial class OmniSettingsTile
{
    private readonly OmniSettingsTileContext _context;

    public OmniSettingsTile() => _context = new OmniSettingsTileContext(StateHasChanged);

    /// <summary>The name of the setting, and the label of the switch or check box beside it.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>What the setting changes, under its name.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>A decorative icon before the name; the name already says what the setting is.</summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>
    /// The control, on the row beside the name. An <see cref="OmniSwitch"/>, <see cref="OmniCheckBox"/>,
    /// <see cref="OmniNullableSwitch"/> or <see cref="OmniNullableCheckBox"/> placed here is labelled by
    /// the name and toggled by a click anywhere on the tile; it receives a generated id when it has none.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Content under the row, for what does not fit beside the name: a slider, a list of actions.</summary>
    [Parameter]
    public RenderFragment? Details { get; set; }
}
