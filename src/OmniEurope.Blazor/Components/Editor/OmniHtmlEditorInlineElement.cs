namespace OmniEurope.Blazor.Components;

/// <summary>
/// Elements of an <see cref="OmniHtmlEditor"/> document that act when clicked in the visual face: a note
/// that opens its editor, a formula, a reference. The click does not move the caret into the element.
/// </summary>
/// <param name="Selector">
/// A CSS selector the clicked element, or one of its ancestors inside the surface, must match:
/// <c>.note[data-marker]</c>, <c>span[data-latex]</c>. The innermost match is the one activated.
/// </param>
/// <param name="Activate">What to do with it; its context can replace or remove the element.</param>
public sealed record OmniHtmlEditorInlineElement(string Selector, Func<OmniHtmlEditorElementContext, Task> Activate);

/// <summary>The element an <see cref="OmniHtmlEditorInlineElement"/> activated, and what can be done with it.</summary>
public sealed class OmniHtmlEditorElementContext
{
    /// <summary>The command the editor context of an activation reports, since no toolbar command ran.</summary>
    private static readonly OmniHtmlEditorCommand ActivationCommand = new("inline-element", OmniHtmlEditorAction.Custom);

    private readonly OmniHtmlEditor _editor;

    internal OmniHtmlEditorElementContext(OmniHtmlEditor editor, OmniHtmlEditorSelectionNode element, string text)
    {
        _editor = editor;
        Element = element;
        Text = text;
    }

    /// <summary>The element: its tag, its classes and its <c>data-*</c> attributes.</summary>
    public OmniHtmlEditorSelectionNode Element { get; }

    /// <summary>The text the element holds, as displayed.</summary>
    public string Text { get; }

    /// <summary>The whole editor, for anything beyond this element (inserting elsewhere, reading the value).</summary>
    public OmniHtmlEditorCommandContext Editor => new(_editor, ActivationCommand);

    /// <summary>
    /// Replaces the element with <paramref name="html"/>, sanitised like any insertion, as one step of the
    /// history. Does nothing when the element has left the document meanwhile.
    /// </summary>
    public Task ReplaceAsync(string html) => _editor.ReplaceActivatedAsync(html);

    /// <summary>Removes the element, as one step of the history.</summary>
    public Task RemoveAsync() => _editor.ReplaceActivatedAsync(string.Empty);
}
