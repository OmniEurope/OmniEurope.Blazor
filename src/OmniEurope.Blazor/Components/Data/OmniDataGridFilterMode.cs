namespace OmniEurope.Blazor.Components;

public enum OmniDataGridFilterMode
{
    /// <summary>One input per filterable column, using the operator declared on the column.</summary>
    Simple,

    /// <summary>One input per filterable column plus an operator selector.</summary>
    SimpleWithMenu,

    /// <summary>Two conditions per column joined by AND or OR, plus apply and clear actions.</summary>
    Advanced
}
