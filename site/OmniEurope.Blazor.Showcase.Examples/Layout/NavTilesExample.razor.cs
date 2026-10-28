using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>Three navigation tiles whose tone and description are parameters.</summary>
public partial class NavTilesExample
{
    /// <summary>The colour of the square and its icon.</summary>
    [Parameter]
    public OmniTone Tone { get; set; } = OmniTone.Accent;

    /// <summary>Whether each tile has a muted description under its title.</summary>
    [Parameter]
    public bool ShowDescription { get; set; } = true;

    private string? Describe(string description) => ShowDescription ? description : null;
}
