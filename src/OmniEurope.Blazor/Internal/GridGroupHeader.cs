namespace OmniEurope.Blazor.Internal;

/// <summary>One group header line to emit above a row, with the state its toggle reflects.</summary>
internal sealed record GridGroupHeader(string Path, string Text, int Level, int Count, bool Expanded);
