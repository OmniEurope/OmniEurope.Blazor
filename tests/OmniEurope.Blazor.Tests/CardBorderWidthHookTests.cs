namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <c>--omni-card-border-width</c> is a documented theme hook (docs/foundation-components.md, "Liseré des
/// cartes"): setting it to 0 removes the frame of the surfaces drawn like a card, and of those only. These
/// tests fix which rules of the stylesheet read it, so adding a reader forces the documented list to follow.
/// </summary>
public sealed class CardBorderWidthHookTests
{
    private const string Hook = "var(--omni-card-border-width";

    // The selectors that read the hook, as documented: the card, the accordion, the date calendar, the
    // grid, spreadsheet, gantt, editors, viewers, diff, mind map, log, code and kanban surfaces.
    private static readonly string[] Readers =
    [
        ".omni-accordion",
        ".omni-calendar",
        ".omni-card",
        ".omni-card::before",
        ".omni-code-block",
        ".omni-code-editor",
        ".omni-code-viewer",
        ".omni-data-grid__frame",
        ".omni-data-grid__viewport",
        ".omni-diff-viewer",
        ".omni-gantt__body",
        ".omni-html-editor",
        ".omni-kanban__card",
        ".omni-log-viewer__viewport",
        ".omni-mindmap-properties",
        ".omni-mindmap__frame",
        ".omni-spreadsheet__formula-bar",
        ".omni-spreadsheet__viewport",
        ".omni-unified-diff__file"
    ];

    // Frames that stay drawn whatever the hook says: they read --omni-border-width.
    private static readonly string[] NonReaders =
    [
        ".omni-settings-tile",
        ".omni-stat-tile",
        ".omni-nav-tile",
        ".omni-fact-tile",
        ".omni-header",
        ".omni-upload__file",
        ".omni-menu",
        ".omni-popover__panel"
    ];

    [Fact]
    public void TheRulesThatReadTheHook_AreExactlyTheDocumentedSurfaces()
    {
        var selectors = ShippedLookTests.Rules()
            .Where(rule => rule.Body.Contains(Hook, StringComparison.Ordinal))
            .SelectMany(rule => rule.Selector.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(Readers.Order(StringComparer.Ordinal), selectors);
    }

    [Fact]
    public void EveryReader_FallsBackToTheGeneralBorderWidth_SoAnUnsetHookChangesNothing()
    {
        foreach (var rule in ShippedLookTests.Rules().Where(rule => rule.Body.Contains(Hook, StringComparison.Ordinal) && !rule.Selector.EndsWith("::before", StringComparison.Ordinal)))
        {
            Assert.Matches(@"var\(--omni-card-border-width, var\(--omni-border-width(, 1px)?\)\)", rule.Body);
        }
    }

    [Fact]
    public void TheTilesAndFields_DoNotReadTheHook()
    {
        foreach (var selector in NonReaders)
        {
            var body = ShippedLookTests.Body(selector);
            Assert.DoesNotContain(Hook, body, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheHook_IsUnsetOnTheRootAndOnEveryThemeScope_SoANestedScopeDoesNotInheritIt()
    {
        Assert.Contains("--omni-card-border-width: initial", ShippedLookTests.ShapeDefaults(), StringComparison.Ordinal);
    }
}
