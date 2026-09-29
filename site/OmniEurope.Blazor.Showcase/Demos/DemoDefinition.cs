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
/// <param name="CapabilityArguments">Values filling the <c>{n}</c> placeholders of a capability, by
/// resource key (a count read from the library rather than written in every language); a key absent
/// from it is shown as written.</param>
public sealed record DemoDefinition(
    string Key,
    Type Component,
    string TitleKey,
    string SummaryKey,
    IReadOnlyList<string> CapabilityKeys,
    IReadOnlyDictionary<string, object[]>? CapabilityArguments = null)
{
    /// <summary>The values filling the placeholders of a capability, empty when it has none.</summary>
    public object[] ArgumentsOf(string capabilityKey) =>
        CapabilityArguments is not null && CapabilityArguments.TryGetValue(capabilityKey, out var arguments)
            ? arguments
            : [];
}
