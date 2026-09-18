using OmniEurope.Blazor.Showcase.Demos;

namespace OmniEurope.Blazor.Showcase.Components.Pages;

/// <summary>A section of the customizer's preview: a gallery entry, and the density it may take of its own.</summary>
/// <param name="demo">The gallery entry shown.</param>
internal sealed class StageSection(DemoDefinition demo)
{
    /// <summary>The gallery entry shown.</summary>
    public DemoDefinition Demo { get; } = demo;

    /// <summary>The section's own density (compact, comfortable, spacious), empty when it inherits the page's.</summary>
    public string Density { get; set; } = string.Empty;
}
