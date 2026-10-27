using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>A settings tile whose icon, description and two content areas are parameters.</summary>
public partial class SettingsSampleTile
{
    /// <summary>Whether the tile shows its icon.</summary>
    [Parameter]
    public bool ShowIcon { get; set; }

    /// <summary>The icon, decorative.</summary>
    [Parameter]
    public OmniIconName IconName { get; set; }

    /// <summary>The name of the setting.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>The line under the name, if any.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>The control beside the name.</summary>
    [Parameter]
    public RenderFragment? Control { get; set; }

    /// <summary>The content under the row of the name.</summary>
    [Parameter]
    public RenderFragment? Below { get; set; }
}
