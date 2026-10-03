using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The only .NET object <c>omni-html-editor.js</c> can call. Kept apart from the component so that
/// the callable surface is internal rather than public API.
/// </summary>
internal sealed class HtmlEditorInteropBridge(OmniHtmlEditor owner)
{
    /// <summary>The surface changed: typing, a paste, a drop. The HTML is sanitised before it is kept.</summary>
    [JSInvokable]
    public Task OnVisualInput(string html) => owner.DispatchAsync(() => owner.HandleVisualInputAsync(html));

    /// <summary>The formatting at the caret changed, as <c>marks|block|align|size</c>.</summary>
    [JSInvokable]
    public Task OnVisualState(string state) => owner.DispatchAsync(() => owner.HandleVisualState(state));

    /// <summary>The caret or the selection settled somewhere new, as the JSON read by <see cref="OmniHtmlEditorSelection"/>.</summary>
    [JSInvokable]
    public Task OnSelectionChanged(string selection) => owner.DispatchAsync(() => owner.HandleSelectionAsync(selection));

    /// <summary>Ctrl+Z, Ctrl+Y or Ctrl+Shift+Z inside the surface.</summary>
    [JSInvokable]
    public Task OnHistoryShortcut(bool redo) => owner.DispatchAsync(() => redo ? owner.RedoAsync() : owner.UndoAsync());

    /// <summary>Ctrl+K inside the surface.</summary>
    [JSInvokable]
    public Task OnLinkShortcut() => owner.DispatchAsync(owner.OpenLinkAsync);

    /// <summary>What a paste or a drop may insert, through the same allow-list as the value.</summary>
    [JSInvokable]
    public string SanitizePaste(string html, string text) => owner.CleanPaste(html, text);

    /// <summary>A key combination of an extension, by its position among the editor's shortcuts.</summary>
    [JSInvokable]
    public Task OnShortcut(int index) => owner.DispatchAsync(() => owner.HandleShortcutAsync(index));

    /// <summary>A click on an inline element of an extension: its position, the element as a selection node, and its text.</summary>
    [JSInvokable]
    public Task OnElementActivated(int index, string element, string text) =>
        owner.DispatchAsync(() => owner.HandleElementActivatedAsync(index, element, text));

    /// <summary>
    /// A right-click or the context-menu key in the surface, at this point of the viewport, with the selection there
    /// and the passage a proofreader flagged under it, if any.
    /// </summary>
    [JSInvokable]
    public Task OnContextMenu(double x, double y, string? selection, string? issue) =>
        owner.DispatchAsync(() => owner.HandleContextMenuAsync(x, y, selection, issue));

    /// <summary>Typing paused: what an extension proposes after the caret, given the text before it, or null.</summary>
    [JSInvokable]
    public Task<string?> OnSuggestionRequested(string textBeforeCaret) => owner.SuggestAsync(textBeforeCaret);

    /// <summary>Typing paused: the blocks of the surface whose text changed, with their languages, checked by the proofreaders.</summary>
    [JSInvokable]
    public Task<string> OnProofreadRequested(string[] texts, string?[] languages) => owner.Proofreading.CheckAsync(texts, languages);
}
