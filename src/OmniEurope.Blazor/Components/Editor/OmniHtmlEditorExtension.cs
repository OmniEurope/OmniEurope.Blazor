namespace OmniEurope.Blazor.Components;

/// <summary>
/// What an application adds to an <see cref="OmniHtmlEditor"/> through its <see cref="OmniHtmlEditor.Extensions"/>:
/// commands and their place in the toolbar, markup the sanitiser must keep, keyboard shortcuts, inline
/// elements that act when clicked, a context menu, and readers for more table file formats.
/// </summary>
/// <remarks>
/// Every member is optional. An extension never touches the surface itself: it acts through the
/// <see cref="OmniHtmlEditorCommandContext"/> given to its commands and the
/// <see cref="OmniHtmlEditorElementContext"/> given to its inline elements, so the value stays sanitised
/// and every change is one step of the editor's history. Several extensions apply in their order.
/// </remarks>
public abstract class OmniHtmlEditorExtension
{
    /// <summary>
    /// The commands this extension brings. <see cref="ArrangeToolbar"/> places them; they are also the
    /// commands its <see cref="Shortcuts"/> may name.
    /// </summary>
    public virtual IReadOnlyList<OmniHtmlEditorCommand> Commands => [];

    /// <summary>
    /// The toolbar once this extension has placed its commands in it. The default appends
    /// <see cref="Commands"/> after a separator; an extension that needs its own order returns a new list,
    /// which may keep, move or leave out the commands it receives.
    /// </summary>
    /// <param name="toolbar">The toolbar so far: the editor's <see cref="OmniHtmlEditor.Commands"/> (or the default one), arranged by the extensions before this one.</param>
    public virtual IReadOnlyList<OmniHtmlEditorCommand> ArrangeToolbar(IReadOnlyList<OmniHtmlEditorCommand> toolbar) =>
        Commands.Count == 0 ? toolbar : [.. toolbar, OmniHtmlEditorCommands.Separator, .. Commands];

    /// <summary>
    /// Markup kept beyond the built-in allow-list, merged with the policies of the other extensions; an
    /// extension may carry nothing else, which is how a host gives an editor its policy. It can only widen
    /// the allow-list, within the limits every <see cref="OmniHtmlSanitizerPolicy"/> keeps.
    /// </summary>
    public virtual OmniHtmlSanitizerPolicy? SanitizerPolicy => null;

    /// <summary>Key combinations that run a command, in the visual face.</summary>
    public virtual IReadOnlyList<OmniHtmlEditorShortcut> Shortcuts => [];

    /// <summary>Elements of the document that do something when clicked, such as a note that opens its editor.</summary>
    public virtual IReadOnlyList<OmniHtmlEditorInlineElement> InlineElements => [];

    /// <summary>
    /// The commands of the menu opened by a right-click or the context-menu key in the visual face. The
    /// editor shows no menu of its own when every extension leaves this empty, so the browser's stays.
    /// </summary>
    public virtual IReadOnlyList<OmniHtmlEditorCommand> ContextMenu => [];

    /// <summary>Readers for table files beyond CSV and TSV, which <see cref="OmniHtmlEditorAction.ImportTable"/> reads itself.</summary>
    public virtual IReadOnlyList<OmniHtmlEditorTableReader> TableReaders => [];

    /// <summary>
    /// Whether this extension needs <see cref="OnSelectionChangedAsync"/>. The surface only reports where
    /// the selection is when someone listens, so an extension that overrides the method says so here.
    /// </summary>
    public virtual bool TracksSelection => false;

    /// <summary>Raised, like <see cref="OmniHtmlEditor.SelectionChanged"/>, once the caret or the selection settles somewhere new.</summary>
    public virtual Task OnSelectionChangedAsync(OmniHtmlEditorSelection selection) => Task.CompletedTask;

    /// <summary>Whether this extension proposes the rest of a sentence through <see cref="SuggestAsync"/>.</summary>
    public virtual bool SuggestsText => false;

    /// <summary>
    /// The text proposed after the caret, once typing has paused for a moment with at least five characters
    /// before the caret in its text: shown dimmed after the caret, Tab types it, Escape or any other key drops
    /// it. It is never part of the value. Null or empty proposes nothing; the first extension that proposes
    /// something wins. An exception is the extension's to handle: an unhandled one only drops the proposal.
    /// </summary>
    /// <param name="textBeforeCaret">Up to the last 200 characters of the text before the caret.</param>
    public virtual Task<string?> SuggestAsync(string textBeforeCaret) => Task.FromResult<string?>(null);

    /// <summary>
    /// The spelling or grammar checker this extension brings, or null: the editor underlines what it flags in the
    /// visual face and offers its corrections on a right-click. Several extensions may each bring one.
    /// </summary>
    public virtual OmniHtmlEditorProofreader? Proofreader => null;
}
