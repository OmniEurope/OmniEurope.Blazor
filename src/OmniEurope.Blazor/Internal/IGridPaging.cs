namespace OmniEurope.Blazor.Components;

/// <summary>The page of a grid as its other collaborators use it, from <see cref="GridPaging{TItem}"/>.</summary>
internal interface IGridPaging
{
    int Page { get; }

    int PageSize { get; }

    int? ChosenPageSize { get; }

    bool Restore(int? pageSize);

    Task ResetToFirstPageAsync();
}
