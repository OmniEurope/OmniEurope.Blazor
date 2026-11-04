namespace OmniEurope.Blazor.Components;

/// <summary>The new text of a run whose text the reader changed.</summary>
/// <param name="RunId">The <see cref="OmniDocumentInline.Id"/> of the run.</param>
/// <param name="Text">The text now in the run, on one line.</param>
public sealed record OmniRunTextChange(string RunId, string Text);
