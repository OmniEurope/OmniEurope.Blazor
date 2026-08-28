namespace OmniEurope.Blazor.Components;

public sealed record OmniDataGridSort(string Key, bool Descending)
{
    /// <summary>Property path to sort on when it differs from the column key.</summary>
    public string? Property { get; init; }
}
