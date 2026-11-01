namespace OmniEurope.Blazor.Components;

/// <summary>
/// What one <see cref="OmniLegend"/> asks for, which the chart needs to draw it below the plot:
/// its name, its own entries (empty to take them from the series) and its position.
/// </summary>
internal sealed record LegendRegistration(
    string Label,
    IReadOnlyList<string> Items,
    OmniLegendPlacement Placement,
    bool AllowToggle = true)
{
    internal bool SameAs(LegendRegistration other) =>
        Label == other.Label
        && Placement == other.Placement
        && AllowToggle == other.AllowToggle
        && Items.SequenceEqual(other.Items);
}
