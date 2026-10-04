namespace OmniEurope.Blazor.Components;

/// <summary>
/// The figure a column computes over a set of rows (<see cref="OmniDataGridColumn{TItem}.Aggregate"/>): in the
/// grid's footer row over every row the filters keep, and in the footer row of each group over the rows of
/// that group.
/// </summary>
public enum OmniDataGridAggregate
{
    /// <summary>Nothing is computed; the default.</summary>
    None,

    /// <summary>The total of the numeric values; empty values are skipped, an empty set totals zero.</summary>
    Sum,

    /// <summary>The mean of the numeric values; empty values are skipped, an empty set shows nothing.</summary>
    Average,

    /// <summary>The smallest value (a number, a date, a text); empty values are skipped.</summary>
    Min,

    /// <summary>The largest value (a number, a date, a text); empty values are skipped.</summary>
    Max,

    /// <summary>The number of rows.</summary>
    Count
}
