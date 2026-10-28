using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>Tabs whose selected panel scrolls on its own, inside a frame of fixed height.</summary>
public partial class ScrollingTabsExample
{
    /// <summary>A CSS selector naming the frame whose wheel turns scroll the panel; null limits it to the panel itself.</summary>
    [Parameter]
    public string? WheelScope { get; set; }
}
