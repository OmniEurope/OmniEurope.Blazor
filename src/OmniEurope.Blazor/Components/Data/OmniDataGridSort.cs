namespace OmniEurope.Blazor.Components;

/// <summary>One sort level of an <see cref="OmniDataGrid{TItem}"/>, as sent to a remote loader in <see cref="OmniDataGridLoadRequest.Sorts"/>.</summary>
/// <param name="Key">Key of the sorted column.</param>
/// <param name="Descending">True for a descending sort, false for ascending.</param>
public sealed record OmniDataGridSort(string Key, bool Descending)
{
    /// <summary>Property path to sort on when it differs from the column key.</summary>
    public string? Property { get; init; }
}
