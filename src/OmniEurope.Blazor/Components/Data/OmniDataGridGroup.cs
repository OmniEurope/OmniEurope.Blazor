namespace OmniEurope.Blazor.Components;

/// <summary>One active grouping level, identified by the key of the column it groups on.</summary>
/// <param name="Key">Key of the column the rows are grouped on.</param>
/// <param name="Descending">True to order the groups in descending order; false (the default) for ascending.</param>
public sealed record OmniDataGridGroup(string Key, bool Descending = false);
