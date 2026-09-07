namespace OmniEurope.Blazor.Showcase.Demos;

/// <summary>
/// One entry of the gallery: a component family, the live demo that exercises it, and the keys of
/// the prose describing what it can do.
/// </summary>
/// <param name="Key">Stable identifier used in the URL and as the tab key.</param>
/// <param name="Component">The demo component, rendered live and read back for its source.</param>
/// <param name="TitleKey">Resource key of the family name.</param>
/// <param name="SummaryKey">Resource key of the one-line summary.</param>
/// <param name="CapabilityKeys">Resource keys of the capabilities listed next to the demo.</param>
public sealed record DemoDefinition(
    string Key,
    Type Component,
    string TitleKey,
    string SummaryKey,
    IReadOnlyList<string> CapabilityKeys);
