namespace OmniEurope.Blazor.Components;

/// <summary>A hunk of a unified diff: a run of lines around a change, introduced by its <c>@@</c> header.</summary>
/// <param name="Header">The header line as written: <c>@@ -12,7 +12,8 @@ section</c>.</param>
/// <param name="OldStart">The first old line the hunk covers.</param>
/// <param name="OldCount">How many old lines it covers.</param>
/// <param name="NewStart">The first new line the hunk covers.</param>
/// <param name="NewCount">How many new lines it covers.</param>
/// <param name="Lines">Its lines, numbered.</param>
public sealed record OmniDiffHunk(string Header, int OldStart, int OldCount, int NewStart, int NewCount, IReadOnlyList<OmniDiffLine> Lines);
