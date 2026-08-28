namespace OmniEurope.Blazor.Components;

public enum OmniDataGridFilterOperator
{
    Contains,
    Equals,
    NotEquals,
    StartsWith,
    EndsWith,
    DoesNotContain,
    GreaterThan,
    GreaterThanOrEquals,
    LessThan,
    LessThanOrEquals,
    IsNull,
    IsNotNull,
    IsEmpty,
    IsNotEmpty,

    /// <summary>
    /// The filter value carries several candidates and a row matches any of them. Read and write
    /// that value with <see cref="OmniDataGridFilterValues"/> rather than splitting it by hand.
    /// </summary>
    In,

    /// <summary>Negation of <see cref="In"/>: a row matches none of the candidates.</summary>
    NotIn
}
