namespace OmniEurope.Blazor.Components;

/// <summary>One line of a unified diff hunk.</summary>
/// <param name="Kind">Unchanged, added, removed, or a note.</param>
/// <param name="Text">The line without its leading marker.</param>
/// <param name="OldNumber">Its number in the old file; null for an added line or a note.</param>
/// <param name="NewNumber">Its number in the new file; null for a removed line or a note.</param>
public sealed record OmniDiffLine(OmniDiffLineKind Kind, string Text, int? OldNumber, int? NewNumber);
