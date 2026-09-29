namespace OmniEurope.Blazor.Components;

/// <summary>Alignment of a column's header and cells (<see cref="OmniDataGridColumn{TItem}.TextAlign"/>), following the writing direction.</summary>
public enum OmniDataGridTextAlign
{
    /// <summary>Aligned at the start of the line. The default; a numeric column without a template aligns at the end instead.</summary>
    Start,

    /// <summary>Centered.</summary>
    Center,

    /// <summary>Aligned at the end of the line.</summary>
    End
}
