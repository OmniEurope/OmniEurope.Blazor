namespace OmniEurope.Blazor.Components;

/// <summary>One active grouping level, identified by the key of the column it groups on.</summary>
public sealed record OmniDataGridGroup(string Key, bool Descending = false);
