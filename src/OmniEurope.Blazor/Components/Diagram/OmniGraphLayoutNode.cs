namespace OmniEurope.Blazor.Components;

/// <summary>A node to lay out: its identifier and the size of its box.</summary>
/// <param name="Id">Identifier the edges refer to. A second node with the same identifier is ignored.</param>
/// <param name="Width">Width of the box; a negative or non-finite value counts as 0.</param>
/// <param name="Height">Height of the box; a negative or non-finite value counts as 0.</param>
public sealed record OmniGraphLayoutNode(string Id, double Width, double Height);
