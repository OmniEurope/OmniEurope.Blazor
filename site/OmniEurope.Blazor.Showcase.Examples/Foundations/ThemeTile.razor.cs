using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>A selectable tile that previews one theme or one palette in a scope of its own.</summary>
public partial class ThemeTile
{
    /// <summary>The name of the choice, as the title of the tile.</summary>
    [Parameter]
    public string Title { get; set; } = string.Empty;

    /// <summary>What the choice looks like, in a few words.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>The theme the tile's scope wears, or null for the shipped one.</summary>
    [Parameter]
    public OmniThemePreset? Preset { get; set; }

    /// <summary>The palette the tile's scope wears, or null for the palette of the theme.</summary>
    [Parameter]
    public OmniThemePalette? Palette { get; set; }

    /// <summary>The half of the look the tile shows.</summary>
    [Parameter]
    public OmniAppearance Appearance { get; set; } = OmniAppearance.Light;

    /// <summary>Whether this tile is the one picked.</summary>
    [Parameter]
    public bool Active { get; set; }

    /// <summary>Raised when the tile is picked.</summary>
    [Parameter]
    public EventCallback OnSelect { get; set; }

    private Task SelectAsync(bool picked) => picked ? OnSelect.InvokeAsync() : Task.CompletedTask;
}
