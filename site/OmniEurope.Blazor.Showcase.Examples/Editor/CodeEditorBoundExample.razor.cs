using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>The text of a code editor, shown in a read-only text area.</summary>
public partial class CodeEditorBoundExample
{
    /// <summary>The code to show.</summary>
    [Parameter]
    public string Code { get; set; } = string.Empty;
}
