using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>The markup an editor binds, shown in a read-only text area.</summary>
public partial class HtmlEditorBoundExample
{
    /// <summary>The HTML to show.</summary>
    [Parameter]
    public string Body { get; set; } = string.Empty;
}
