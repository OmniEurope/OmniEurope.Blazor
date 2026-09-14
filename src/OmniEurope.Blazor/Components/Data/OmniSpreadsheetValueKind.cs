namespace OmniEurope.Blazor.Components;

/// <summary>What a spreadsheet cell holds once its input is read and its formula computed.</summary>
public enum OmniSpreadsheetValueKind
{
    /// <summary>Nothing typed. Counts as zero in arithmetic and is skipped by the aggregates.</summary>
    Empty,

    /// <summary>A number, typed or computed.</summary>
    Number,

    /// <summary>Text that does not read as a number.</summary>
    Text,

    /// <summary>A formula that cannot be computed, with its code in <see cref="OmniSpreadsheetValue.Text"/>.</summary>
    Error
}
