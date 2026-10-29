using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>Three navigation tiles whose tone, description and the disabled state of the last one are parameters.</summary>
public partial class NavTilesExample
{
    /// <summary>The colour of the square and its icon.</summary>
    [Parameter]
    public OmniTone Tone { get; set; } = OmniTone.Accent;

    /// <summary>Whether each tile has a muted description under its title.</summary>
    [Parameter]
    public bool ShowDescription { get; set; } = true;

    /// <summary>Whether the last tile is disabled: no link, attenuated, announced as unavailable.</summary>
    [Parameter]
    public bool DisableLast { get; set; }

    private string? Describe(string description) => ShowDescription ? description : null;
}
