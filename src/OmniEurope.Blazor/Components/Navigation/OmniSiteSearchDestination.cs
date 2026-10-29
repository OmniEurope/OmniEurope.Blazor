namespace OmniEurope.Blazor.Components;

/// <summary>A page <see cref="OmniSiteSearch"/> can lead to.</summary>
/// <param name="Text">What the page is called: the text listed, matched and highlighted.</param>
/// <param name="Href">Where picking it leads: an address relative to the base address of the application, or an absolute one, checked by the package's address policy.</param>
/// <param name="Group">The heading the page is listed under (a section, a family); null (the default) for none. The groups come in the order of their first page in the list given to the search.</param>
/// <param name="Keywords">Other words that find the page without being shown (synonyms, former names, the components it holds); null (the default) for none. They are matched like <paramref name="Text"/>, ignoring case and accents.</param>
/// <param name="Icon">An icon drawn before the text; null (the default) for none.</param>
public sealed record OmniSiteSearchDestination(
    string Text,
    string Href,
    string? Group = null,
    IReadOnlyList<string>? Keywords = null,
    OmniIconName? Icon = null);
