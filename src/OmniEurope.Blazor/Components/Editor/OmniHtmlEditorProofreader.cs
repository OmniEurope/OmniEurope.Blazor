namespace OmniEurope.Blazor.Components;

/// <summary>
/// A spelling or grammar checker an <see cref="OmniHtmlEditorExtension"/> brings to an <see cref="OmniHtmlEditor"/>
/// through its <see cref="OmniHtmlEditorExtension.Proofreader"/>. The editor holds no engine and no dictionary: it gives
/// the proofreader the text of its visual face, block by block with each block's language, underlines what comes back
/// without changing the document, and offers the proofreader's corrections on a right-click.
/// </summary>
/// <remarks>
/// The editor asks again once typing pauses, only for the blocks that changed. An exception of the proofreader drops
/// its answer and leaves the document as it is; a proofreader handles its own failures when it wants them seen.
/// </remarks>
public abstract class OmniHtmlEditorProofreader
{
    /// <summary>The passages to underline in <paramref name="texts"/>, in any order.</summary>
    /// <param name="texts">The blocks to check, never empty.</param>
    /// <param name="cancellationToken">Cancelled when the editor no longer needs the answer.</param>
    public abstract Task<IReadOnlyList<OmniHtmlEditorProofreadingIssue>> CheckAsync(
        IReadOnlyList<OmniHtmlEditorProofreadingText> texts, CancellationToken cancellationToken);

    /// <summary>
    /// The replacements proposed for a passage, best first; asked when its menu opens, which is where a costly
    /// suggestion belongs. The menu shows five at most. The default proposes none.
    /// </summary>
    /// <param name="text">The block the passage is in.</param>
    /// <param name="issue">The passage, its <see cref="OmniHtmlEditorProofreadingIssue.TextIndex"/> being 0.</param>
    /// <param name="cancellationToken">Cancelled when the menu closes first.</param>
    public virtual Task<IReadOnlyList<string>> SuggestAsync(
        OmniHtmlEditorProofreadingText text, OmniHtmlEditorProofreadingIssue issue, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>([]);

    /// <summary>Whether the menu offers "Ignore all", which calls <see cref="IgnoreAllAsync"/>.</summary>
    public virtual bool CanIgnoreAll => false;

    /// <summary>
    /// The passage is no longer to be flagged anywhere (for the session, or longer as the proofreader decides); the
    /// editor then checks its text again.
    /// </summary>
    /// <param name="passage">The flagged passage.</param>
    /// <param name="language">The language of its block, or null.</param>
    public virtual Task IgnoreAllAsync(string passage, string? language) => Task.CompletedTask;

    /// <summary>Whether the menu offers "Add to dictionary", which calls <see cref="AddToDictionaryAsync"/>.</summary>
    public virtual bool CanAddToDictionary => false;

    /// <summary>The passage joins the user's dictionary; the editor then checks its text again.</summary>
    /// <param name="passage">The flagged passage.</param>
    /// <param name="language">The language of its block, or null.</param>
    public virtual Task AddToDictionaryAsync(string passage, string? language) => Task.CompletedTask;
}
