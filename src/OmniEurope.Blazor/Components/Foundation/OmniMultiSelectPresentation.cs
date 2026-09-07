namespace OmniEurope.Blazor.Components;

/// <summary>
/// How a multi-select occupies the page.
/// </summary>
public enum OmniMultiSelectPresentation
{
    /// <summary>An always-open list of <see cref="OmniMultiSelect{TValue}.VisibleRows"/> rows.</summary>
    List,

    /// <summary>A single-line control that opens its list on demand.</summary>
    Compact
}
