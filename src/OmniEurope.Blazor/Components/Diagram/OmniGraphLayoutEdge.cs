namespace OmniEurope.Blazor.Components;

/// <summary>A directed edge to lay out, from one node identifier to another.</summary>
/// <param name="From">The node the edge leaves.</param>
/// <param name="To">The node the edge points to.</param>
/// <remarks>An edge naming an unknown node, a loop on one node and a repeated edge are ignored.</remarks>
public sealed record OmniGraphLayoutEdge(string From, string To);
