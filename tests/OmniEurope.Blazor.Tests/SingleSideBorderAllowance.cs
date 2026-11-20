namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The only rules of the shipped stylesheet allowed to draw a line on one side: the body lines of
/// <c>OmniDataGrid</c> that the app picks through <c>GridLines</c> (lines between rows, lines between columns).
/// They make a real table grid and are not liserés (owner decision, 2026-11-20). This is a closed allowance,
/// not a backlog: the header, footer and group-footer lines are gone (a tinted band replaces them) and must
/// not come back; nothing else may be added.
/// </summary>
internal static class SingleSideBorderAllowance
{
    internal const string Reason = "real table grid, owner decision 2026-11-20";

    internal static readonly string[] Selectors =
    [
        ".omni-data-grid:is(.omni-data-grid--lines-vertical, .omni-data-grid--lines-both) :is(th, td)",
        ":where(.omni-data-grid:not(.omni-data-grid--lines-none):not(.omni-data-grid--lines-vertical), .omni-data-grid--lines-both) tbody :is(td, th)",
    ];
}
