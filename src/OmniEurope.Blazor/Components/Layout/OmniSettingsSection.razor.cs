namespace OmniEurope.Blazor.Components;

/// <summary>
/// One theme of a settings page: an <see cref="OmniCard"/> with a level 2 heading, an optional one-line
/// description, and its settings inside.
/// </summary>
public partial class OmniSettingsSection
{
    /// <summary>The theme the section gathers, shown as its heading.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>One line on what the settings of the section change.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>
    /// Sets the tiles two per row (recette R-060): each takes half the width of the section,
    /// and a tile with <see cref="OmniSettingsTile.FullWidth"/> the whole row; under about 24rem per tile
    /// they stack. The section itself spans its container with no width cap. False by default: one tile
    /// per row, as before.
    /// </summary>
    [Parameter]
    public bool Paired { get; set; }

    /// <summary>
    /// Draws the tiles flat: no fill and no frame of their own, so they do not make a box inside the card
    /// of the section. In a column they are separated by a hairline rule; in a <see cref="Paired"/> section,
    /// by spacing alone. The hover tint stays, and so does a tint while a control of the tile has the
    /// focus, so the tile still shows what a click reaches. False by default: framed tiles, as before.
    /// </summary>
    [Parameter]
    public bool Flat { get; set; }

    /// <summary>The settings, typically one <see cref="OmniSettingsTile"/> each.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
