namespace OmniEurope.Blazor.Components;

/// <summary>
/// The section of a settings page that holds the actions that cannot be undone (deleting a project,
/// resetting an account): an <see cref="OmniSettingsSection"/> whose rim and heading take the palette's
/// danger colour, in every theme and both modes, and a region named by its level 2 heading. Place one
/// <see cref="OmniSettingsDangerAction"/> per action inside.
/// </summary>
public partial class OmniSettingsDangerZone
{
    private readonly string _generatedId = $"omni-settings-danger-zone-{Guid.NewGuid():N}";

    /// <summary>The heading of the section. Null, the default, is the localized "Danger zone".</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>One line on what the actions of the section do, under the heading.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>The actions, one <see cref="OmniSettingsDangerAction"/> each.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string TitleId => $"{Id ?? _generatedId}-title";
}
