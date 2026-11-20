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
        ".omni-accordion__item + .omni-accordion__item",
        ".omni-app-menu__card > .omni-button.omni-app-menu__item",
        ".omni-app-menu__extra",
        ".omni-app-menu__row",
        ".omni-card__footer",
        ".omni-card__header",
        ".omni-code-block__header",
        ".omni-code-editor__status",
        ".omni-code-viewer__header",
        ".omni-code-viewer__panel",
        ".omni-data-grid tbody > .omni-data-grid__group-footer > td",
        ".omni-data-grid thead > tr > *",
        ".omni-data-grid:is(.omni-data-grid--lines-vertical, .omni-data-grid--lines-both) :is(th, td)",
        ".omni-data-grid__bar--footer",
        ".omni-data-grid__bar--header",
        ".omni-data-list__item",
        ".omni-description-list > :nth-child(n + 2)",
        ".omni-description-list--responsive > :nth-child(n + 2)",
        ".omni-description-list__item",
        ".omni-dialog__footer",
        ".omni-dialog__header",
        ".omni-diff-viewer__bar",
        ".omni-document-viewer__toolbar",
        ".omni-gantt__names",
        ".omni-gantt__names > .omni-gantt__names-head",
        ".omni-html-editor__link",
        ".omni-html-editor__menu-separator",
        ".omni-html-editor__palette",
        ".omni-html-editor__panel",
        ".omni-html-editor__preview",
        ".omni-html-editor__separator",
        ".omni-html-editor__toolbar",
        ".omni-mindmap__menu-separator",
        ".omni-panel-menu__children",
        ".omni-settings-section--flat .omni-settings-tile + .omni-settings-tile",
        ".omni-tabs__strip",
        ".omni-tabs__tab",
        ".omni-tree__level",
        ".omni-unified-diff__body",
        ".omni-wizard__actions",
        ":where(.omni-data-grid) tfoot td",
        ":where(.omni-data-grid:not(.omni-data-grid--lines-none):not(.omni-data-grid--lines-vertical), .omni-data-grid--lines-both) tbody :is(td, th)",
    ];
}
