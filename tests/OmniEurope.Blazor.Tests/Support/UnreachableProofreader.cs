using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>A proofreader whose corrections and "ignore all" fail, as an unreachable service's do.</summary>
internal abstract class UnreachableProofreader : OmniHtmlEditorProofreader
{
    public override bool CanIgnoreAll => true;

    public override Task<IReadOnlyList<string>> SuggestAsync(
        OmniHtmlEditorProofreadingText text, OmniHtmlEditorProofreadingIssue issue, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("dictionnaire absent");

    public override Task IgnoreAllAsync(string passage, string? language) => throw new IOException("lecture seule");
}
