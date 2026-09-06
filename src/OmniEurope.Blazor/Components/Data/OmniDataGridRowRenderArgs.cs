namespace OmniEurope.Blazor.Components;

/// <summary>
/// Passed to <c>RowRender</c> so the host can style a row or decide whether it can expand,
/// without emitting an inline style attribute.
/// </summary>
public sealed class OmniDataGridRowRenderArgs<TItem>
{
    internal OmniDataGridRowRenderArgs(TItem item, int index)
    {
        Item = item;
        Index = index;
    }

    public TItem Item { get; }

    public int Index { get; }

    /// <summary>Extra CSS classes applied to the row.</summary>
    public string? CssClass { get; set; }

    /// <summary>Set to <c>false</c> to hide the expand control of this row.</summary>
    public bool Expandable { get; set; } = true;

    /// <summary>Set to <c>false</c> to make the row ignore clicks and selection.</summary>
    public bool Selectable { get; set; } = true;
}
