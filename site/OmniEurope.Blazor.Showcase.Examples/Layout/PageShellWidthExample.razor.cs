using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>A scrolling main area at one content width.</summary>
public partial class PageShellWidthExample
{
    /// <summary>The width of the content column.</summary>
    [Parameter]
    public OmniLayoutWidth Width { get; set; } = OmniLayoutWidth.Content;

    private string MainId => $"page-shell-width-{Width.ToString().ToLowerInvariant()}-main";

    private string TitleId => $"page-shell-width-{Width.ToString().ToLowerInvariant()}-title";
}
