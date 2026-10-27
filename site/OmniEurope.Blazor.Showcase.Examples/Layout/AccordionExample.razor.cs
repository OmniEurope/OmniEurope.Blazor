using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>An accordion of four rows whose opening mode, body retention, heading level and help marks are parameters.</summary>
public partial class AccordionExample
{
    /// <summary>Whether several rows can be open together; otherwise opening a row closes its neighbour.</summary>
    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>Whether the body of a row stays rendered once it has been opened.</summary>
    [Parameter]
    public bool KeepContent { get; set; }

    /// <summary>The heading level that wraps the toggle button of each row.</summary>
    [Parameter]
    public OmniHeadingLevel Level { get; set; } = OmniHeadingLevel.H3;

    /// <summary>Whether the first two rows carry a "?" help mark.</summary>
    [Parameter]
    public bool ShowHelp { get; set; } = true;

    /// <summary>Whether the first row starts open.</summary>
    [Parameter]
    public bool FirstOpen { get; set; } = true;

    private string AccordionKey => $"{Multiple}-{KeepContent}-{Level}-{FirstOpen}-{ShowHelp}";

    private string? HelpText(string text) => ShowHelp ? text : null;
}
