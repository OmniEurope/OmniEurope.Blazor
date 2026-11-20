using System.Text.RegularExpressions;
using OmniEurope.Blazor.Components;

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

    // An inset shadow layer: the keyword `inset` with the two offsets that follow the colour-less lengths.
    [GeneratedRegex(@"(?<![\w-])inset(?![\w-])", RegexOptions.CultureInvariant)]
    private static partial Regex InsetKeyword();

    [GeneratedRegex(@"(?<![\w.#-])[+-]?(?:\d+\.?\d*|\.\d+)(?:[a-z]+|%)?(?![\w.-])", RegexOptions.CultureInvariant)]
    private static partial Regex Length();

    // The layers of a shadow list: split on the commas outside parentheses (a colour such as rgb(0 0 0 / 5%) stays whole).
    private static IEnumerable<string> ShadowLayers(string value) => SplitSelectorList(value);

    private static bool IsZeroLength(string length) =>
        double.TryParse(new string(length.TakeWhile(ch => char.IsDigit(ch) || ch is '.' or '-' or '+').ToArray()),
            System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var number) && number == 0;

    // A highlight line drawn inside one edge (inset 0 1px 0 ...) is a liseré too: any inset layer with a non-zero offset.
    private static string[] InsetLines() => OffsetInsetLayers(ShippedLookTests.Rules()
            .SelectMany(rule => rule.Body.Split(';').Select(declaration => (Source: rule.Selector, Value: declaration[(declaration.IndexOf(':', StringComparison.Ordinal) + 1)..]))))
        .Select(layer => $"{layer.Source} => {layer.Layer}")
        .Distinct(StringComparer.Ordinal)
        .Order(StringComparer.Ordinal)
        .ToArray();

    // The inset layers of the given shadow values that are moved off centre, with their lengths (x, y, then blur when given).
    private static IEnumerable<(string Source, string Layer, string[] Lengths)> OffsetInsetLayers(IEnumerable<(string Source, string Value)> values) => values
        .Where(item => InsetKeyword().IsMatch(item.Value))
        .SelectMany(item => ShadowLayers(item.Value)
            .Where(layer => InsetKeyword().IsMatch(layer))
            .Select(layer => (Source: item.Source.Replace("\n", " ", StringComparison.Ordinal), Layer: layer.Trim(), Lengths: Length().Matches(InsetKeyword().Replace(StripColors(layer), " ")).Take(3).Select(m => m.Value).ToArray()))
            .Where(layer => layer.Lengths.Length >= 2 && layer.Lengths.Take(2).Any(offset => !IsZeroLength(offset))));

    // The theme presets are written into the page at run time, outside the stylesheet: a sharp inset layer moved off
    // centre (no blur) draws a line along one edge there too. A blurred one (a pressed or carved relief) shades a
    // whole side and is not a line.
    private static string[] PresetInsetLines() => OffsetInsetLayers(OmniThemePresets.All
            .SelectMany(preset => preset.Shape.Concat(preset.DarkShape).Select(token => (Source: $"{preset.Name} {token.Key}", token.Value))))
        .Where(layer => layer.Lengths.Length == 2 || IsZeroLength(layer.Lengths[2]))
        .Select(layer => $"{layer.Source} => {layer.Layer}")
        .Distinct(StringComparer.Ordinal)
        .Order(StringComparer.Ordinal)
        .ToArray();

    // Removes functional colours (rgb(...), color-mix(...), var(...)) so their numbers are not read as offsets.
    private static string StripColors(string layer)
    {
        var text = layer;
        while (true)
        {
            var next = Regex.Replace(text, @"[\w-]+\([^()]*\)", " ", RegexOptions.CultureInvariant);
            if (next == text)
            {
                return text;
            }

            text = next;
        }
    }

    [Fact]
    public void NoRule_DrawsAnInsetLineOnOneSide()
    {
        var found = InsetLines();
        Assert.True(found.Length == 0, "Inset box-shadows with a non-zero offset draw a line on one side (liserés are forbidden; use an inset 0 0 0 Npx ring): " + string.Join(" | ", found));
    }

    [Fact]
    public void NoThemePreset_DrawsASharpInsetLineOnOneSide()
    {
        var found = PresetInsetLines();
        Assert.True(found.Length == 0, "Theme tokens with a sharp inset shadow moved off centre draw a line on one side (liserés are forbidden): " + string.Join(" | ", found));
    }
}
