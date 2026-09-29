namespace OmniEurope.Blazor.Components;

/// <summary>How an <see cref="OmniDataGrid{TItem}"/> builds a column filter (its <c>FilterMode</c> parameter).</summary>
public enum OmniDataGridFilterMode
{
    /// <summary>One input per filterable column, using the operator declared on the column.</summary>
    Simple,

    /// <summary>One input per filterable column plus an operator selector.</summary>
    SimpleWithMenu,

    /// <summary>Two conditions per column joined by AND or OR, plus apply and clear actions.</summary>
    Advanced
}
