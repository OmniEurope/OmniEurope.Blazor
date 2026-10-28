using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Diagram;

/// <summary>The stored JSON of a mind map document, shown read-only.</summary>
public partial class MindMapJsonExample
{
    /// <summary>The map whose stored format is shown.</summary>
    [Parameter]
    public OmniMindMapDocument Document { get; set; } = MindMapSample.Create();

    private string Json => Document.ToJson();
}
