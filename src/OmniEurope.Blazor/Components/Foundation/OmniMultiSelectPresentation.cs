namespace OmniEurope.Blazor.Components;

/// <summary>
/// How the multiple-choice filter of a grid column (<see cref="OmniDataGridFilterMultiSelect"/>)
/// occupies its place.
/// </summary>
public enum OmniMultiSelectPresentation
{
    /// <summary>An always-open list of check boxes.</summary>
    List,

    /// <summary>A single-line control that opens its list on demand.</summary>
    Compact
}
