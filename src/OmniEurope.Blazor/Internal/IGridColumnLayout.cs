using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>The column widths of a grid as its other collaborators use them, from <see cref="GridColumnLayout{TItem}"/>.</summary>
/// <typeparam name="TItem">The type of the rows.</typeparam>
internal interface IGridColumnLayout<TItem>
{
    IReadOnlyDictionary<string, string?> Widths { get; }

    void Forget(string key);

    void Restore(IEnumerable<KeyValuePair<string, string?>> widths);

    bool HasEdgeHandle(OmniDataGridColumnDefinition<TItem> column);

    string TableMinimumWidth();

    string Signature();

    object Specs();

    string[]? LoadedTexts(OmniDataGridColumnDefinition<TItem> column);
}
