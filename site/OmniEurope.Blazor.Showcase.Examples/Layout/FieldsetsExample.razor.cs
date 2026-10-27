using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>A group of fields whose legend, disabled, collapsible and collapsed states are parameters.</summary>
public partial class FieldsetsExample
{
    /// <summary>The legend: any content, usually a short title.</summary>
    [Parameter]
    public string LegendText { get; set; } = "Contact details";

    /// <summary>Whether the browser disables every field of the group.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether the legend becomes the handle of a disclosure.</summary>
    [Parameter]
    public bool Collapsible { get; set; }

    /// <summary>Whether a collapsible group starts folded; read at the first render only.</summary>
    [Parameter]
    public bool Collapsed { get; set; }

    private string ContactName { get; set; } = string.Empty;

    private string ContactEmail { get; set; } = string.Empty;

    private bool Newsletter { get; set; } = true;

    private bool Saved { get; set; }

    private string GroupKey => $"{Collapsible}-{Collapsed}";
}
