namespace OmniEurope.Blazor.Internal;

/// <summary>
/// A measured label size and what was measured: when the text, size or weight changes the
/// signature no longer matches and the estimate is used until the browser measures again.
/// </summary>
internal sealed record MindMapMeasuredLabel(string Signature, double Width, double Height);
