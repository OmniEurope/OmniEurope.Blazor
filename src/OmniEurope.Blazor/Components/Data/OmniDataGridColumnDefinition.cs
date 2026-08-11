using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Components;

internal sealed class OmniDataGridColumnDefinition<TItem>
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    public required Func<TItem, object?> Value { get; init; }
    public RenderFragment<TItem>? Template { get; init; }
    public RenderFragment<TItem>? EditTemplate { get; init; }
    public Func<TItem, string, bool>? FilterPredicate { get; init; }
    public Func<object?, string>? Format { get; init; }
    public bool Sortable { get; init; } = true;
    public bool Filterable { get; init; }
    public OmniDataGridFilterOperator FilterOperator { get; init; }
    public bool Visible { get; init; } = true;
    public bool Resizable { get; init; }
    public OmniDataGridColumnWidth Width { get; init; }
}
