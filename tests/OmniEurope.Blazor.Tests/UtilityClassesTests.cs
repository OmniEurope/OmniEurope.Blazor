// SPDX-License-Identifier: EUPL-1.2
using System.Text.RegularExpressions;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The omni-u-* utilities are for the markup an application writes around the components. They are
/// additive: each rule targets one utility class of its own, never a component's class, so adding the
/// layer cannot restyle anything that already renders.
/// </summary>
public sealed partial class UtilityClassesTests
{
    [GeneratedRegex(@"(?<selectors>[^{}]+)\{(?<body>[^{}]*)\}")]
    private static partial Regex Rule();

    [Fact]
    public void EveryUtilityRule_TargetsOneUtilityClassAndNothingElse()
    {
        var utilities = UtilityRules().ToList();

        Assert.True(utilities.Count > 90, $"Only {utilities.Count} utility rules were found: the scan is broken.");
        Assert.All(utilities, rule => Assert.Matches(@"^\.omni-u-[a-z0-9-]+$", rule.Selector));
    }

    [Fact]
    public void SpacingUtilities_ReadTheSpacingTokens()
    {
        var spacing = UtilityRules().Where(rule => Regex.IsMatch(rule.Selector, @"^\.omni-u-[mp][tbsexy]?-(xs|sm|md|lg|xl|2xl)$")).ToList();

        Assert.Equal(12 * 6, spacing.Count);
        Assert.All(spacing, rule => Assert.Contains("var(--omni-space-", rule.Body, StringComparison.Ordinal));
    }

    [Fact]
    public void NoOtherRule_UsesAUtilityClass()
    {
        var styles = Styles();
        var outside = Rule().Matches(styles)
            .Select(match => match.Groups["selectors"].Value.Trim())
            .Where(selectors => selectors.Contains(".omni-u-", StringComparison.Ordinal))
            .SelectMany(selectors => selectors.Split(','))
            .Select(selector => selector.Trim())
            .Where(selector => !Regex.IsMatch(selector, @"^\.omni-u-[a-z0-9-]+$"))
            .ToList();

        Assert.Empty(outside);
    }

    private static IEnumerable<(string Selector, string Body)> UtilityRules() =>
        Rule().Matches(Styles())
            .Select(match => (Selector: match.Groups["selectors"].Value.Trim(), Body: match.Groups["body"].Value))
            .Where(rule => rule.Selector.StartsWith(".omni-u-", StringComparison.Ordinal));

    private static string Styles()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "src", "OmniEurope.Blazor")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        // Comments are dropped so the one heading the utilities is not read as part of a selector.
        return Regex.Replace(
            File.ReadAllText(Path.Combine(directory.FullName, "src", "OmniEurope.Blazor", "wwwroot", "omnieurope.blazor.css")),
            @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
    }
}
