namespace OmniEurope.Blazor.Components;

/// <summary>
/// Whether a bar of an <see cref="OmniDataGrid{TItem}"/> carries the export buttons, and on which side:
/// <see cref="OmniDataGrid{TItem}.HeaderBarExport"/> and <see cref="OmniDataGrid{TItem}.FooterBarExport"/>.
/// </summary>
public enum OmniDataGridBarExport
{
    /// <summary>The bar carries no export button.</summary>
    None,

    /// <summary>The buttons at the start of the bar (left in a left-to-right page), its content after them.</summary>
    Start,

    /// <summary>The buttons at the end of the bar (right in a left-to-right page), its content before them.</summary>
    End
}
