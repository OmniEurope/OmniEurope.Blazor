namespace OmniEurope.Blazor.Components;

/// <summary>One crumb of the trail held by <see cref="OmniBreadcrumbService"/>.</summary>
/// <param name="Text">What the crumb reads.</param>
/// <param name="Href">Where the crumb leads; null for a crumb that is not a link.</param>
/// <param name="Loading">
/// True while the text is not known yet, an entity name still loading for instance: the trail shows a
/// placeholder in its place, and a header titled by that crumb shows one instead of its title.
/// </param>
public sealed record OmniBreadcrumbEntry(string Text, string? Href = null, bool Loading = false);
