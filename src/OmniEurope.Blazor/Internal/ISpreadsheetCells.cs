
namespace OmniEurope.Blazor.Components;

/// <summary>
/// The cells of the sheet a <see cref="SpreadsheetFormula"/> reads, and the nesting of the computation
/// in progress. Implemented by the sheet's evaluator.
/// </summary>
internal interface ISpreadsheetCells
{
    /// <summary>The current nesting of the computation in progress, kept by <see cref="SpreadsheetFormula"/>.</summary>
    int Depth { get; set; }

    /// <summary>Whether the position is inside the sheet.</summary>
    bool Contains(int row, int column);

    /// <summary>The value of a cell, computed once and remembered.</summary>
    OmniSpreadsheetValue Evaluate(int row, int column);
}
