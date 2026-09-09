namespace OmniEurope.Blazor.Components;

/// <summary>
/// What a column title does when it is wider than its column.
/// </summary>
public enum OmniDataGridHeaderWrap
{
    /// <summary>It runs onto a second line, and the header band grows to hold it.</summary>
    Wrap,

    /// <summary>It is cut with an ellipsis, and the header band stays one line tall.</summary>
    Truncate
}
