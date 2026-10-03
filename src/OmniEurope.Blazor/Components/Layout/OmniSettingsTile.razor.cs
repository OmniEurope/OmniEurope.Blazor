namespace OmniEurope.Blazor.Components;

/// <summary>
/// One setting of an <see cref="OmniSettingsSection"/>: an optional icon, the name and description on the
/// left, the control on the right, optional details underneath. A switch or check box placed as the
/// control is labelled by the name and toggled by a click anywhere on the tile.
/// </summary>
public partial class OmniSettingsTile
{
    private readonly OmniSettingsTileContext _context;

    /// <summary>Creates the tile and the context its switch or check box joins; Blazor calls it when the component is rendered.</summary>
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
    /// The control, on the row beside the name. An <see cref="OmniSwitch{TValue}"/> or <see cref="OmniCheckBox{TValue}"/>,
    /// bound to a bool or a bool?, placed here is labelled by
    /// the name and toggled by a click anywhere on the tile; it receives a generated id when it has none.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Content under the row, for what does not fit beside the name: a slider, a list of actions.</summary>
    [Parameter]
    public RenderFragment? Details { get; set; }

    /// <summary>
    /// The density of this component and of what it holds (control heights, paddings, gaps), over
    /// the one it inherits from its theme scope or section. Null, the default, inherits it.
    /// </summary>
    [Parameter]
    public OmniDensity? Density { get; set; }

    /// <summary>
    /// In a <see cref="OmniSettingsSection.Paired"/> section, takes the whole row instead of half of it (a
    /// wide setting: a list, an editor). No effect in a section that is not paired.
    /// </summary>
    [Parameter]
    public bool FullWidth { get; set; }
}
