namespace OmniEurope.Blazor.Showcase.Components.Pages;

/// <summary>A section of the customizer's preview: a demo or an example, and the density it may take of its own.</summary>
/// <param name="key">The stable key of the section, used in its ids.</param>
/// <param name="strings">The marker type of the resources holding the section title.</param>
/// <param name="titleKey">The resource key of the section title.</param>
/// <param name="component">The component the section renders, with its default parameters.</param>
internal sealed class StageSection(string key, Type strings, string titleKey, Type component)
{
    /// <summary>The stable key of the section, used in its ids.</summary>
    public string Key { get; } = key;

    /// <summary>The marker type of the resources holding the section title.</summary>
    public Type Strings { get; } = strings;

    /// <summary>The resource key of the section title.</summary>
    public string TitleKey { get; } = titleKey;

    /// <summary>The component the section renders.</summary>
    public Type Component { get; } = component;

    /// <summary>The section's own density (compact, comfortable, spacious), empty when it inherits the page's.</summary>
    public string Density { get; set; } = string.Empty;
}
