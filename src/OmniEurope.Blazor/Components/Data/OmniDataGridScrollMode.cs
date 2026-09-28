namespace OmniEurope.Blazor.Components;

/// <summary>How an <see cref="OmniDataGrid{TItem}"/> presents more rows than fit on screen.</summary>
public enum OmniDataGridScrollMode
{
    /// <summary><see cref="OmniDataGrid{TItem}.PageSize"/> rows at a time, with a pager. The default.</summary>
    Paged,

    /// <summary>
    /// Only the rows the viewport can show are rendered, under a continuous scrollbar over the whole
    /// row count; no pager.
    /// </summary>
    Virtual,

    /// <summary>Every row rendered at once, without pager or virtualization.</summary>
    All
}
