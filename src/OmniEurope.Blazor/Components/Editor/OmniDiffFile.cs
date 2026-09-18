namespace OmniEurope.Blazor.Components;

/// <summary>A file of a unified diff, with its hunks.</summary>
/// <param name="OldPath">Its path before the change, without the <c>a/</c> prefix; null for a new file or a bare hunk.</param>
/// <param name="NewPath">Its path after the change, without the <c>b/</c> prefix; null for a removed file or a bare hunk.</param>
/// <param name="Status">What the change did to it.</param>
/// <param name="IsBinary">Whether git reported it as binary, with no line to show.</param>
/// <param name="Hunks">Its hunks, in order.</param>
public sealed record OmniDiffFile(string? OldPath, string? NewPath, OmniDiffFileStatus Status, bool IsBinary, IReadOnlyList<OmniDiffHunk> Hunks)
{
    /// <summary>The path to show: the new one, else the old one; null for a bare hunk without headers.</summary>
    public string? Path => NewPath ?? OldPath;

    /// <summary>The number of added lines.</summary>
    public int Additions => Hunks.Sum(hunk => hunk.Lines.Count(line => line.Kind == OmniDiffLineKind.Added));

    /// <summary>The number of removed lines.</summary>
    public int Deletions => Hunks.Sum(hunk => hunk.Lines.Count(line => line.Kind == OmniDiffLineKind.Removed));
}
