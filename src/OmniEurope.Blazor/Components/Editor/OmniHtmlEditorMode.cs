namespace OmniEurope.Blazor.Components;

/// <summary>How <see cref="OmniHtmlEditor"/> presents its value.</summary>
public enum OmniHtmlEditorMode
{
    /// <summary>The formatted document, edited in place (WYSIWYG).</summary>
    Visual,

    /// <summary>The HTML source, edited as text, with an optional sanitised preview.</summary>
    Source
}
