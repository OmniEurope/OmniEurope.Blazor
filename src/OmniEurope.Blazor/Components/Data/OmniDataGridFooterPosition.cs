namespace OmniEurope.Blazor.Components;

/// <summary>
/// Where the footer row is rendered.
/// </summary>
public enum OmniDataGridFooterPosition
{
    /// <summary>After the rows, in a real table footer. The default.</summary>
    Bottom,

    /// <summary>
    /// Directly under the header. What a footer holding a creation form needs: at the bottom it
    /// sits past every row, so adding an entry to a full grid starts with a scroll.
    /// </summary>
    Top
}
