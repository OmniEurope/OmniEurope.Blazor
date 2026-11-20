namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The rules of the shipped stylesheet that still draw a line on one side only (a liseré), recorded on
/// 2026-11-19 when the guard was added. Each one is to be removed, never added to: a liseré is forbidden.
/// Which ones go, and with what in their place, is the owner's call (row separators, tab indicators, ...).
/// </summary>
internal static class SingleSideBorderBacklog
{
    internal static readonly string[] Selectors =
    [
        ".omni-calendar__foot",
        ".omni-character-palette__separator",
        ".omni-data-grid tbody > .omni-data-grid__group-footer > td",
        ".omni-data-grid thead > tr > *",
        ".omni-data-grid:is(.omni-data-grid--lines-vertical, .omni-data-grid--lines-both) :is(th, td)",
        ".omni-multi-select-compact__footer",
        ".omni-password__toggle",
        ".omni-picker__body .omni-time",
        ".omni-progress__segment + .omni-progress__segment",
        ".omni-split-button:not(.omni-split-button--ghost):not(.omni-split-button--ghostdanger) > .omni-split-button__toggle",
        ".omni-steps__button",
        ":where(.omni-data-grid) tfoot td",
        ":where(.omni-data-grid:not(.omni-data-grid--lines-none):not(.omni-data-grid--lines-vertical), .omni-data-grid--lines-both) tbody :is(td, th)",
    ];
}
