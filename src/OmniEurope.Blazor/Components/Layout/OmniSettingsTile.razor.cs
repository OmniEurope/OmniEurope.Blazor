namespace OmniEurope.Blazor.Components;

public partial class OmniSettingsTile
{
    /// <summary>The name of the setting.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>What the setting changes, under its name.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>A decorative icon before the name; the name already says what the setting is.</summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>The control, on the row beside the name.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Content under the row, for what does not fit beside the name: a slider, a list of actions.</summary>
    [Parameter]
    public RenderFragment? Details { get; set; }
}
