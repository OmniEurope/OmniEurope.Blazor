namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What a backdrop of the catalogue decides: the field drawn behind the page of a theme scope, as the
/// tokens it lays over a theme. A backdrop writes no colour of its own: its stops are drawn from the
/// colour tokens of the palette in force, so it follows any palette.
/// </summary>
/// <param name="Name">The backdrop name, as theme names are: French, never translated, and the key the preference keeps.</param>
/// <param name="Description">One line describing the backdrop.</param>
/// <param name="Shape">
/// The tokens laid over both modes: <c>--omni-backdrop</c> (the CSS field, pinned to the viewport by the
/// scope), its <c>--omni-backdrop-*</c> stops, <c>--omni-scope-motion</c> when it moves and
/// <c>--omni-scope-canvas</c> when a script draws it on a canvas.
/// </param>
/// <param name="DarkShape">The tokens laid over the dark mode only, after <paramref name="Shape"/>.</param>
/// <param name="CanvasScript">
/// The module that draws the field on the scope's canvas (<see cref="OmniModules"/>), for the canvas
/// kinds the backdrop names in <c>--omni-scope-canvas</c>; null for a field drawn in CSS alone.
/// </param>
internal sealed record BackdropDefinition(
    string Name,
    string Description,
    IReadOnlyDictionary<string, string> Shape,
    IReadOnlyDictionary<string, string> DarkShape,
    string? CanvasScript = null)
{
    /// <summary>The token dictionary of a backdrop, compared by name.</summary>
    internal static Dictionary<string, string> Tokens(params (string Name, string Value)[] tokens) =>
        tokens.ToDictionary(token => token.Name, token => token.Value, StringComparer.Ordinal);
}
