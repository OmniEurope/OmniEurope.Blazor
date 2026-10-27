using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Feedback;

/// <summary>A skeleton whose shape, line count, accessible name and loaded state are parameters.</summary>
public partial class SkeletonExample
{
    /// <summary>Text lines, a picture block or an avatar.</summary>
    [Parameter]
    public OmniSkeletonShape Shape { get; set; } = OmniSkeletonShape.Text;

    /// <summary>The number of lines, from 1 to 10, for the text shape only.</summary>
    [Parameter]
    public int LineCount { get; set; } = 3;

    /// <summary>Whether the skeleton is a named status region instead of a decoration.</summary>
    [Parameter]
    public bool Named { get; set; } = true;

    /// <summary>Whether the content the skeleton stands for has arrived.</summary>
    [Parameter]
    public bool Loaded { get; set; }
}
