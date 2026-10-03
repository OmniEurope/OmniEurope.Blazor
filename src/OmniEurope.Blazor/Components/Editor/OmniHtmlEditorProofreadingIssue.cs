namespace OmniEurope.Blazor.Components;

/// <summary>A passage an <see cref="OmniHtmlEditorProofreader"/> flags in one of the texts it was given.</summary>
/// <param name="TextIndex">The position of the text in the list given to <see cref="OmniHtmlEditorProofreader.CheckAsync"/>.</param>
/// <param name="Start">Where the passage starts in that text, in UTF-16 characters.</param>
/// <param name="Length">The length of the passage, in UTF-16 characters; an issue outside the text is ignored.</param>
/// <param name="Kind">What was found, which sets how the passage is underlined.</param>
/// <param name="Message">An explanation shown at the top of the passage's menu, or null.</param>
public sealed record OmniHtmlEditorProofreadingIssue(
    int TextIndex,
    int Start,
    int Length,
    OmniHtmlEditorProofreadingKind Kind = OmniHtmlEditorProofreadingKind.Spelling,
    string? Message = null);
