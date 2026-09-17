namespace OmniEurope.Blazor.Components;

/// <summary>Where <see cref="OmniGraphLayout"/> put every node, and the size of the whole drawing.</summary>
/// <param name="Positions">The centre of each node, by identifier; the drawing starts at 0, 0.</param>
/// <param name="Width">Width of the drawing, boxes included.</param>
/// <param name="Height">Height of the drawing, boxes included.</param>
public sealed record OmniGraphLayoutResult(IReadOnlyDictionary<string, OmniGraphPoint> Positions, double Width, double Height);
