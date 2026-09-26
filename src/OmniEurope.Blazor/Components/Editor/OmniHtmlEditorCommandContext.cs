namespace OmniEurope.Blazor.Components;

/// <summary>What a custom command of <see cref="OmniHtmlEditor"/> can read and change.</summary>
/// <remarks>
/// Every change goes through the editor: it is sanitised with the same allow-list as typed and
/// pasted content, recorded in the undo history and raised through <c>ValueChanged</c>.
/// </remarks>
public sealed class OmniHtmlEditorCommandContext
{
    private readonly OmniHtmlEditor _editor;

    internal OmniHtmlEditorCommandContext(OmniHtmlEditor editor, OmniHtmlEditorCommand command)
    {
        _editor = editor;
        Command = command;
    }

    /// <summary>The command being run.</summary>
    public OmniHtmlEditorCommand Command { get; }

    /// <summary>The current, sanitised value, including what was typed a moment ago.</summary>
    public string Html => _editor.CurrentHtml;

    /// <summary>The mode the editor is in.</summary>
    public OmniHtmlEditorMode Mode => _editor.CurrentMode;

    /// <summary>
    /// The last selection the visual face reported, or null in the source face or while nothing
    /// listens: the surface reports it only for <see cref="OmniHtmlEditor.SelectionChanged"/> or a
    /// command with <see cref="OmniHtmlEditorCommand.Pressed"/>.
    /// </summary>
    public OmniHtmlEditorSelection? Selection => _editor.CurrentSelection;

    /// <summary>
    /// The editable surface of the visual face, for a host script that changes the document itself
    /// (then calls <see cref="CommitDomAsync"/>); null in the source face.
    /// </summary>
    public ElementReference? SurfaceElement => _editor.SurfaceReference;

    /// <summary>
    /// Reads the visual surface after a host script changed it, sanitises it with the editor's
    /// allow-list and policy, and commits it: one undo step, and <c>ValueChanged</c> when it differs.
    /// What the sanitiser removed is also removed from the surface. Does nothing in the source face.
    /// </summary>
    public Task CommitDomAsync() => _editor.CommitDomAsync();

    /// <summary>Inserts <paramref name="html"/> at the caret, replacing the selection.</summary>
    public Task InsertHtmlAsync(string html) => _editor.InsertHtmlAsync(html);

    /// <summary>Replaces the whole value.</summary>
    public Task SetHtmlAsync(string html) => _editor.ReplaceHtmlAsync(html);

    /// <summary>Runs a built-in action, as its toolbar command would.</summary>
    /// <param name="action">The action; <see cref="OmniHtmlEditorAction.Custom"/> and <see cref="OmniHtmlEditorAction.Separator"/> do nothing.</param>
    /// <param name="argument">The block tag for <see cref="OmniHtmlEditorAction.BlockFormat"/> (<c>p</c>, <c>h1</c> to <c>h4</c>), the size for <see cref="OmniHtmlEditorAction.FontSize"/> (<c>small</c>, <c>normal</c>, <c>large</c>, <c>xlarge</c>) or the address for <see cref="OmniHtmlEditorAction.Link"/>.</param>
    public Task ExecuteAsync(OmniHtmlEditorAction action, string? argument = null) =>
        _editor.RunBuiltInAsync(action, argument);
}
