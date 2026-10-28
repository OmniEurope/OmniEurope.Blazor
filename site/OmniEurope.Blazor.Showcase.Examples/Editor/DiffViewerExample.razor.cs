using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>Two commands compared side by side.</summary>
public partial class DiffViewerExample
{
    /// <summary>Compares the original with the modified command; turned off, with itself.</summary>
    [Parameter]
    public bool Changed { get; set; } = true;

    private string Modified => CodeReadingSamples.Command(Changed);
}
