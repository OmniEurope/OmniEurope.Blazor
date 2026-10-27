using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Feedback;

/// <summary>A gauge of each tone, then an indeterminate one, as bars or as rings.</summary>
public partial class ProgressSamplesExample
{
    /// <summary>Whether the gauges are rings instead of bars.</summary>
    [Parameter]
    public bool Circular { get; set; }
}
