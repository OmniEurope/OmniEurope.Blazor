using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>The HTML of an editor, rendered outside it.</summary>
public partial class HtmlEditorRenderedExample
{
    /// <summary>The HTML to render. It must come from the editor, which has sanitised it, or from a text the page wrote.</summary>
    [Parameter]
    public string Body { get; set; } = HtmlEditorSamples.Letter;

    private MarkupString RenderedBody => new(Body);
}
