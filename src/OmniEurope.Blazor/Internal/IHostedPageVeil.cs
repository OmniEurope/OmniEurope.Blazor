namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What a main area needs from a page veil declared in it: the element it draws over its own frame. The main
/// area holds its veils through this contract, so it never names the veil component, which reads the main area.
/// </summary>
internal interface IHostedPageVeil
{
    /// <summary>The element of the veil, keyed by the veil so the main area that draws it keeps it across renders.</summary>
    RenderFragment Surface { get; }
}
