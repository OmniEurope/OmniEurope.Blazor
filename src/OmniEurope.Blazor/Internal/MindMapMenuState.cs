namespace OmniEurope.Blazor.Internal;

/// <summary>
/// An open context menu: the node it was opened on (null for the canvas background), where it sits
/// on the canvas in pixels, and the map point under the pointer, where a new node would go.
/// </summary>
internal sealed record MindMapMenuState(string? NodeId, double Left, double Top, double MapX, double MapY);
