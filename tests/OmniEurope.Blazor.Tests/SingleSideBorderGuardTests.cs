using System.Text.RegularExpressions;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Liserés (a border or a line on one side only of an element) are forbidden by the owner's rule. These tests
/// read the shipped stylesheet: the shell surfaces (header, sidebar panel) draw none, and no rule beyond the
/// recorded ones may start drawing one, so the list below can only shrink.
/// </summary>
public sealed partial class SingleSideBorderGuardTests
{
    // A declaration that draws a line on one side: a side shorthand or width with a value other than 0 or none.
    [GeneratedRegex(@"(?<![\w-])border-(?:top|right|bottom|left|inline-start|inline-end|block-start|block-end)(?:-width)?\s*:\s*(?<value>[^;]+)", RegexOptions.CultureInvariant)]
    private static partial Regex SideBorder();

    private static bool DrawsALine(string value)
    {
        var trimmed = value.Trim();
        return trimmed is not ("0" or "none" or "0 none" or "none 0" or "initial" or "unset" or "inherit")
            && !trimmed.StartsWith("0 ", StringComparison.Ordinal);
    }

    private static string[] SingleSideSelectors() => ShippedLookTests.Rules()
        .Where(rule => SideBorder().Matches(rule.Body).Any(match => DrawsALine(match.Groups["value"].Value)))
        .SelectMany(rule => SplitSelectorList(rule.Selector))
        .Distinct(StringComparer.Ordinal)
        .Order(StringComparer.Ordinal)
        .ToArray();

    // The selectors of a list, split on the commas outside :is(), :where() and :not().
    private static IEnumerable<string> SplitSelectorList(string list)
    {
        var depth = 0;
        var start = 0;
        for (var index = 0; index < list.Length; index++)
        {
            switch (list[index])
            {
                case '(':
                    depth++;
                    break;
                case ')':
                    depth--;
                    break;
                case ',' when depth == 0:
                    yield return list[start..index].Trim();
                    start = index + 1;
                    break;
            }
        }

        yield return list[start..].Trim();
    }

    [Theory]
    [InlineData(".omni-header")]
    [InlineData(".omni-sidebar__panel")]
    [InlineData(".omni-sidebar--end > .omni-sidebar__panel")]
    public void TheShellSurfaces_DrawNoLineOnOneSide(string selector)
    {
        var body = ShippedLookTests.Body(selector);
        Assert.DoesNotMatch(SideBorder(), body);
    }

    [Fact]
    public void NoRule_StartsDrawingALineOnOneSide()
    {
        var found = SingleSideSelectors();
        var added = found.Except(SingleSideBorderBacklog.Selectors, StringComparer.Ordinal).ToArray();
        Assert.True(added.Length == 0, "New single-side borders (liserés are forbidden): " + string.Join(" | ", added));
    }

    [Fact]
    public void TheBacklog_ListsOnlyRulesThatStillDrawOne()
    {
        var found = SingleSideSelectors();
        var gone = SingleSideBorderBacklog.Selectors.Except(found, StringComparer.Ordinal).ToArray();
        Assert.True(gone.Length == 0, "Remove from SingleSideBorderBacklog, they no longer draw a line: " + string.Join(" | ", gone));
    }
}
