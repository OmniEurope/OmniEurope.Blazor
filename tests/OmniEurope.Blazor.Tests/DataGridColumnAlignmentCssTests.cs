namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <c>TextAlign</c> of a grid column reaches its header and its cells as
/// <c>omni-data-grid__column--align-*</c>. The cell default (<c>text-align: start</c> on every th and td
/// of the table) must not outrank it, or the column alignment has no visible effect.
/// </summary>
public sealed class DataGridColumnAlignmentCssTests
{
    private const string CellDefault = ".omni-data-grid__table :is(th, td)";

    [Theory]
    [InlineData("start")]
    [InlineData("center")]
    [InlineData("end")]
    public void ColumnAlignment_OutranksTheCellDefault(string alignment)
    {
        Assert.Equal("start", ShippedLookTests.Value(ShippedLookTests.Body(CellDefault), "text-align"));

        var rule = Assert.Single(ShippedLookTests.Rules(), rule =>
            rule.Selector.EndsWith(".omni-data-grid__column--align-" + alignment, StringComparison.Ordinal)
            && rule.Body.Contains("text-align", StringComparison.Ordinal));
        Assert.Equal(alignment, ShippedLookTests.Value(rule.Body, "text-align"));
        Assert.True(
            Specificity(rule.Selector).CompareTo(Specificity(CellDefault)) > 0,
            $"'{rule.Selector}' {Specificity(rule.Selector)} does not outrank '{CellDefault}' {Specificity(CellDefault)}.");
    }

    [Theory]
    [InlineData(CellDefault, 0, 1, 1)]
    [InlineData(".omni-data-grid__column--align-end", 0, 1, 0)]
    [InlineData(".omni-data-grid__table .omni-data-grid__column--align-end", 0, 2, 0)]
    [InlineData(":where(.omni-icon)", 0, 0, 0)]
    [InlineData("#page tr:nth-child(2n) > td::before", 1, 1, 3)]
    public void Specificity_FollowsTheSelectorsLevel4Rules(string selector, int ids, int classes, int types) =>
        Assert.Equal((ids, classes, types), Specificity(selector));

    /// <summary>
    /// Specificity of one complex selector, for the forms this stylesheet uses: <c>:where()</c> counts
    /// nothing, <c>:is()</c>, <c>:not()</c> and <c>:has()</c> count their most specific argument.
    /// </summary>
    private static (int Ids, int Classes, int Types) Specificity(string selector)
    {
        var (ids, classes, types) = (0, 0, 0);
        var index = 0;
        while (index < selector.Length)
        {
            var character = selector[index];
            if (character == '#' || character == '.')
            {
                if (character == '#') ids++; else classes++;
                index = NameEnd(selector, index + 1);
            }
            else if (character == '[')
            {
                classes++;
                index = selector.IndexOf(']', index) + 1;
            }
            else if (character == ':')
            {
                var element = index + 1 < selector.Length && selector[index + 1] == ':';
                var start = index + (element ? 2 : 1);
                var end = NameEnd(selector, start);
                var name = selector[start..end];
                if (end < selector.Length && selector[end] == '(')
                {
                    var close = ClosingParenthesis(selector, end);
                    if (name is "is" or "not" or "has")
                    {
                        var most = selector[(end + 1)..close].Split(',').Select(part => Specificity(part.Trim())).Max();
                        (ids, classes, types) = (ids + most.Ids, classes + most.Classes, types + most.Types);
                    }
                    else if (name != "where")
                    {
                        classes++;
                    }

                    index = close + 1;
                }
                else
                {
                    if (element) types++; else classes++;
                    index = end;
                }
            }
            else if (char.IsLetter(character))
            {
                types++;
                index = NameEnd(selector, index + 1);
            }
            else
            {
                index++;
            }
        }

        return (ids, classes, types);
    }

    private static int NameEnd(string selector, int index)
    {
        while (index < selector.Length && (char.IsLetterOrDigit(selector[index]) || selector[index] is '-' or '_')) index++;
        return index;
    }

    private static int ClosingParenthesis(string selector, int open)
    {
        var depth = 0;
        for (var index = open; index < selector.Length; index++)
        {
            if (selector[index] == '(') depth++;
            else if (selector[index] == ')' && --depth == 0) return index;
        }

        throw new FormatException($"Unbalanced parentheses in '{selector}'.");
    }
}
