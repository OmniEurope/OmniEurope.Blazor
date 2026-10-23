namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The rule the tiles that can be one whole link or one whole button share (<see cref="Components.OmniStatTile"/>,
/// <see cref="Components.OmniFactTile"/>): an address and a click handler never come together, and the
/// address goes through the package's URI policy.
/// </summary>
internal static class OmniTileAction
{
    /// <summary>Checks the pair and returns the address to write, or null when the tile is not a link.</summary>
    /// <param name="component">The component's name, for the message.</param>
    /// <param name="href">The tile's <c>Href</c>.</param>
    /// <param name="hasOnClick">Whether the tile has an <c>OnClick</c> handler.</param>
    /// <returns>The address the policy accepted, null without an address.</returns>
    /// <exception cref="ArgumentException">An address and a click handler are both given.</exception>
    /// <exception cref="InvalidOperationException">The address uses a scheme the policy refuses, or names another host.</exception>
    internal static string? SafeHref(string component, string? href, bool hasOnClick)
    {
        var hasHref = !string.IsNullOrWhiteSpace(href);
        if (hasHref && hasOnClick)
        {
            throw new ArgumentException($"{component} takes either Href (a link) or OnClick (a button), not both.", "Href");
        }

        return hasHref ? OmniUriPolicy.EnsureSafe(href, "Href") : null;
    }
}
