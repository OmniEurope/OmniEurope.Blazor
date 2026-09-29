namespace OmniEurope.Blazor.Components;

/// <summary>
/// How a filter value is compared with a cell. In memory, the cell and the value are compared as text
/// (the cell's <c>ToString()</c>, empty for <c>null</c>), with the grid's case and diacritics settings
/// (see <see cref="OmniDataGridFilterText"/>). A remote loader receives the operator as is in
/// <see cref="OmniDataGridFilter"/> and applies it itself.
/// </summary>
public enum OmniDataGridFilterOperator
{
    /// <summary>The cell text contains the value. Also the in-memory fallback for an unknown operator.</summary>
    Contains,

    /// <summary>The cell text equals the value.</summary>
    Equals,

    /// <summary>The cell text differs from the value.</summary>
    NotEquals,

    /// <summary>The cell text starts with the value.</summary>
    StartsWith,

    /// <summary>The cell text ends with the value.</summary>
    EndsWith,

    /// <summary>The cell text does not contain the value.</summary>
    DoesNotContain,

    /// <summary>
    /// The cell is greater than the value. In memory both sides are compared as numbers when both parse
    /// in the current culture, else as dates, else as text.
    /// </summary>
    GreaterThan,

    /// <summary>The cell is greater than or equal to the value (same comparison as <see cref="GreaterThan"/>).</summary>
    GreaterThanOrEquals,

    /// <summary>The cell is less than the value (same comparison as <see cref="GreaterThan"/>).</summary>
    LessThan,

    /// <summary>The cell is less than or equal to the value (same comparison as <see cref="GreaterThan"/>).</summary>
    LessThanOrEquals,

    /// <summary>The cell value is <c>null</c>; the filter value is ignored.</summary>
    IsNull,

    /// <summary>The cell value is not <c>null</c>; the filter value is ignored.</summary>
    IsNotNull,

    /// <summary>The cell text is empty, which includes a <c>null</c> cell; the filter value is ignored.</summary>
    IsEmpty,

    /// <summary>The cell text is not empty; the filter value is ignored.</summary>
    IsNotEmpty,

    /// <summary>
    /// The filter value carries several candidates and a row matches any of them. Read and write
    /// that value with <see cref="OmniDataGridFilterValues"/> rather than splitting it by hand.
    /// </summary>
    In,

    /// <summary>Negation of <see cref="In"/>: a row matches none of the candidates.</summary>
    NotIn
}
