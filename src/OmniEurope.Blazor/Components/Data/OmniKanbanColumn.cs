namespace OmniEurope.Blazor.Components;

/// <summary>A column of an <see cref="OmniKanban{TItem}"/>.</summary>
/// <param name="Key">The value <see cref="OmniKanban{TItem}.ColumnOf"/> returns for the items of this column.</param>
/// <param name="Title">What the column header shows, and how the announcements name the column.</param>
public sealed record OmniKanbanColumn(string Key, string Title);
