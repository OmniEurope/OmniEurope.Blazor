namespace OmniEurope.Blazor.Internal;

/// <summary>A run of text, and whether it matches the searched text.</summary>
internal readonly record struct OmniTextSegment(string Text, bool Matched);
