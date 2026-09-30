namespace OmniEurope.Blazor.Components;

/// <summary>What the cells of an exported column hold, so a spreadsheet gets numbers and dates, not their text.</summary>
public enum OmniTableExportValueKind
{
    /// <summary>Text: only <see cref="OmniTableExportCell.Text"/> is set.</summary>
    Text,

    /// <summary>A number: <see cref="OmniTableExportCell.Number"/> is set on the cells that hold one.</summary>
    Number,

    /// <summary>A date, with or without a time: <see cref="OmniTableExportCell.Date"/> is set on the cells that hold one.</summary>
    Date,

    /// <summary>True or false: <see cref="OmniTableExportCell.Boolean"/> is set on the cells that hold one.</summary>
    Boolean
}
