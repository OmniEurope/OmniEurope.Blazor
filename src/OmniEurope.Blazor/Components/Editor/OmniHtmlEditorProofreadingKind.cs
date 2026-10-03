namespace OmniEurope.Blazor.Components;

/// <summary>What an <see cref="OmniHtmlEditorProofreadingIssue"/> found, which sets how the passage is underlined.</summary>
public enum OmniHtmlEditorProofreadingKind
{
    /// <summary>A word the proofreader does not know: a wavy underline in the danger colour.</summary>
    Spelling,

    /// <summary>A construction the proofreader questions (agreement, a repeated word): a wavy underline in the info colour.</summary>
    Grammar
}
