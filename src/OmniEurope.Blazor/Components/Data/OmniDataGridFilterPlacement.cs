namespace OmniEurope.Blazor.Components;

/// <summary>Where an <see cref="OmniDataGrid{TItem}"/> puts the filter editor of its columns (its <c>FilterPlacement</c> parameter).</summary>
public enum OmniDataGridFilterPlacement
{
    /// <summary>
    /// In a menu opened from the funnel of each column title: the header stays one row. The menu holds the same
    /// editor as the row, except a date column, which shows its From and To range alone (its <c>HeaderMenuDateFilter</c>
    /// picks the typed field instead).
    /// </summary>
    HeaderMenu,

    /// <summary>In a row of filters under the header, one control per column, a date column as one text input.</summary>
    Row
}
