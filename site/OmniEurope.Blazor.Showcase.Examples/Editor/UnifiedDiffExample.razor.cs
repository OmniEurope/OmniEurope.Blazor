using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>A unified diff with an action of the host on each file.</summary>
public partial class UnifiedDiffExample
{
    /// <summary>Opens the files of the diff expanded; turned off, only their header shows.</summary>
    [Parameter]
    public bool ExpandedByDefault { get; set; } = true;

    private string? OpenedFile { get; set; }
}
