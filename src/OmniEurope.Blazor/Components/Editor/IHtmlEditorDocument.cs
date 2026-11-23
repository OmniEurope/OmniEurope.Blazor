namespace OmniEurope.Blazor.Components;

/// <summary>
/// What the contexts a command or an inline element receives (<see cref="OmniHtmlEditorCommandContext"/>,
/// <see cref="OmniHtmlEditorElementContext"/>) reach of their editor: the value, the face, the selection, and
/// the changes that go through its sanitiser and history. Implemented explicitly by <see cref="OmniHtmlEditor"/>,
/// so the public contexts never name the component that creates them.
/// </summary>
internal interface IHtmlEditorDocument
{
    /// <summary>The current, sanitised value, empty when there is none.</summary>
    string CurrentHtml { get; }

    /// <summary>The face shown.</summary>
    OmniHtmlEditorMode CurrentMode { get; }

    /// <summary>The last selection the visual face reported, or null.</summary>
    OmniHtmlEditorSelection? CurrentSelection { get; }

    /// <summary>The surface element of the visual face, or null in the source face.</summary>
    ElementReference? SurfaceReference { get; }

    /// <summary>Takes in a surface a host script changed, already on the dispatcher.</summary>
    Task CommitSurfaceAsync();

    /// <summary>Inserts sanitised HTML at the caret.</summary>
    Task InsertHtmlAsync(string html);

    /// <summary>Replaces the whole value.</summary>
    Task ReplaceHtmlAsync(string html);

    /// <summary>Runs a built-in action, as its toolbar command would.</summary>
    Task RunBuiltInAsync(OmniHtmlEditorAction action, string? argument);

    /// <summary>Replaces the innermost element around the selection that matches the selector.</summary>
    Task<bool> ReplaceClosestAsync(string selector, string html);

    /// <summary>The content before and after the caret, as balanced sanitised HTML.</summary>
    Task<OmniHtmlCaretSplit> GetHtmlAroundCaretAsync();

    /// <summary>The text of the selection.</summary>
    Task<string> GetSelectedTextAsync();

    /// <summary>Types plain text over the selection.</summary>
    Task InsertTextAsync(string text);

    /// <summary>Replaces the activated inline element.</summary>
    Task ReplaceActivatedAsync(string html);

    /// <summary>Replaces the text of the activated inline element, keeping the element.</summary>
    Task SetActivatedTextAsync(string text);
}
