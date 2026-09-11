namespace OmniEurope.Blazor.Components;

public partial class OmniSettingsSection
{
    /// <summary>The theme the section gathers, shown as its heading.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>One line on what the settings of the section change.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>The settings, typically one <see cref="OmniSettingsTile"/> each.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
