using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What the collaborators of <see cref="OmniHtmlEditor"/> (its faces, script bridge, context menu, panels,
/// proofreading and extension dispatch) reach of the editor that owns them. Implemented explicitly by the
/// editor, so its collaborators depend on this contract rather than on the component, and name no other
/// collaborator through it.
/// </summary>
internal interface IHtmlEditorHost : IHtmlEditorDocument
{
    /// <summary>Whether the value is closed to editing, read-only or disabled.</summary>
    bool IsLocked { get; }

    /// <summary>Whether the editor is disabled.</summary>
    bool Disabled { get; }

    /// <summary>The height of the source text area in rows, and the minimum height of the visual surface.</summary>
    int Rows { get; }

    /// <summary>Whether the editor was disposed.</summary>
    bool IsDisposed { get; }

    /// <summary>Whether the surface reports where the selection is.</summary>
    bool TracksSelection { get; }

    /// <summary>The value as it stands, null included.</summary>
    string? EditorValue { get; }

    /// <summary>The extensions taken together.</summary>
    HtmlEditorExtensionSet ExtensionSet { get; }

    /// <summary>The editable surface of the visual face.</summary>
    ElementReference SurfaceElement { get; }

    /// <summary>The text area of the source face.</summary>
    ElementReference SourceElement { get; }

    /// <summary>The identifier of the editing surface.</summary>
    string SurfaceId { get; }

    /// <summary>The logger of the editor.</summary>
    ILogger Logger { get; }

    /// <summary>The toolbar, arranged by each extension.</summary>
    IReadOnlyList<OmniHtmlEditorCommand> ArrangedCommands { get; }

    /// <summary>The HTML sanitised with the allow-list and the extensions' policies.</summary>
    string Clean(string? html);

    /// <summary>A paste sanitised with the allow-list and the extensions' policies.</summary>
    string CleanPaste(string? html, string? text);

    /// <summary>Takes a sanitised value as the new one, recorded as one undo step when it differs.</summary>
    Task CommitAsync(string value);

    /// <summary>Renders the editor again, already on the dispatcher.</summary>
    void Rerender();

    /// <summary>Renders the editor again, from any thread.</summary>
    Task RenderAsync();

    /// <summary>Runs work on the renderer's dispatcher, then renders, unless the editor is gone.</summary>
    Task DispatchAsync(Func<Task> work);

    /// <summary>Runs synchronous work on the renderer's dispatcher, then renders, unless the editor is gone.</summary>
    Task DispatchAsync(Action work);

    /// <summary>Runs a command of the toolbar, the extensions or the context menu.</summary>
    Task RunAsync(OmniHtmlEditorCommand command);

    /// <summary>Takes in what was typed in the visual surface and not yet reported.</summary>
    Task CaptureVisualAsync();

    /// <summary>Drops the corrections still being asked for the context menu.</summary>
    void CancelProofreadingSuggestions();

    /// <summary>The visual surface changed.</summary>
    Task HandleVisualInputAsync(string html);

    /// <summary>The formatting at the caret changed.</summary>
    void HandleVisualState(string state);

    /// <summary>The selection settled somewhere new.</summary>
    Task HandleSelectionAsync(string json);

    /// <summary>Undoes the last change.</summary>
    Task UndoAsync();

    /// <summary>Redoes the last undone change.</summary>
    Task RedoAsync();

    /// <summary>Opens the link field.</summary>
    Task OpenLinkAsync();

    /// <summary>Runs the command an extension shortcut names.</summary>
    Task HandleShortcutAsync(int index);

    /// <summary>Hands the activation of an inline element to the extension that owns it.</summary>
    Task HandleElementActivatedAsync(int index, string element, string text);

    /// <summary>Opens the context menu at the pointer.</summary>
    Task HandleContextMenuAsync(double x, double y, string? selection, string? issue);

    /// <summary>The first proposal of the extensions that suggest text, or null.</summary>
    Task<string?> SuggestAsync(string textBeforeCaret);

    /// <summary>Checks the changed blocks with the proofreaders; null when nothing was checked.</summary>
    Task<string?> ProofreadAsync(string[] texts, string?[] languages);
}
