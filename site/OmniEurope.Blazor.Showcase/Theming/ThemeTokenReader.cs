using System.Text.RegularExpressions;

namespace OmniEurope.Blazor.Showcase.Theming;

/// <summary>
/// Reads the token catalogue out of the library stylesheet itself.
/// </summary>
/// <remarks>
/// The editor deliberately owns no hand-written list of tokens. A duplicated list would drift the
/// day a token is added to the stylesheet and nobody remembers to mirror it here, and the editor
/// would then silently stop exposing part of the theme. Parsing the shipped file keeps the two in
/// step by construction.
/// </remarks>
public sealed partial class ThemeTokenReader(HttpClient http)
{
    private const string StylesheetPath = "_content/OmniEurope.Blazor/omnieurope.blazor.css";

    // The theme scope with no value: the block that declares the neutral theme hooks.
    private const string ThemeScope = "[data-omni-theme]";

    private IReadOnlyList<ThemeToken>? _cached;

    /// <summary>
    /// Fetches and parses the stylesheet once, then serves the parsed catalogue.
    /// </summary>
    public async Task<IReadOnlyList<ThemeToken>> ReadAsync(CancellationToken cancellationToken = default)
    {
        if (_cached is not null)
        {
            return _cached;
        }

        var css = await http.GetStringAsync(StylesheetPath, cancellationToken).ConfigureAwait(false);
        _cached = Parse(css);
        return _cached;
    }

    /// <summary>
    /// Extracts the declarations of every top-level block whose selector list names <c>:root</c>, and
    /// of the bare theme-scope block (<c>[data-omni-theme]</c> alone), in the order the file declares
    /// them so the editor presents them the way the stylesheet is written. The shipped colours and
    /// shape live in generated blocks that also name the theme scopes
    /// (<c>:root, [data-omni-theme="light"]</c>); the theme hooks only some themes set (Givre's colour
    /// field, frosted cards, Aplat's plain grids) are declared with their neutral value on the bare
    /// scope block only, and are part of a theme like the rest. A token declared twice keeps its last
    /// value, as the cascade does.
    /// </summary>
    public static IReadOnlyList<ThemeToken> Parse(string css)
    {
        // Release ships the stylesheet minified on one line, so nothing here may depend on line breaks.
        var uncommented = Comment().Replace(css, string.Empty);
        var blocks = RootBlock().Matches(uncommented);
        var scaled = DensityScaled(blocks);
        var tokens = new List<ThemeToken>();
        var positions = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Match block in blocks)
        {
            var selectors = block.Groups["selector"].Value.Split(',', StringSplitOptions.TrimEntries);
            if (!selectors.Contains(":root", StringComparer.Ordinal) && selectors is not [ThemeScope])
            {
                continue;
            }

            foreach (Match declaration in Declaration().Matches(block.Groups["body"].Value))
            {
                var name = declaration.Groups["name"].Value;
                var group = scaled.Contains(name) ? ThemeTokenGroup.Spacing : GroupOf(name);
                var token = new ThemeToken(name, declaration.Groups["value"].Value.Trim(), group);
                if (positions.TryGetValue(name, out var index))
                {
                    tokens[index] = token;
                }
                else
                {
                    positions[name] = tokens.Count;
                    tokens.Add(token);
                }
            }
        }

        return tokens;
    }

    /// <summary>
    /// The tokens a density block declares: every size the density scales, whatever its name. Read
    /// from the stylesheet like the rest, so a size added to the density blocks is filed with the
    /// others without a list to keep in step here.
    /// </summary>
    private static HashSet<string> DensityScaled(MatchCollection blocks)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match block in blocks)
        {
            if (!block.Groups["selector"].Value.Contains("[data-omni-density=", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (Match declaration in Declaration().Matches(block.Groups["body"].Value))
            {
                names.Add(declaration.Groups["name"].Value);
            }
        }

        return names;
    }

    /// <summary>
    /// The family of a token the density does not scale. A theme's shape (radii, borders, the card
    /// fill, the button press, fonts, shadows and the floating layer) never lands among the colours:
    /// the colours are the palette's, and a visitor looking for the shape of a theme looks elsewhere.
    /// </summary>
    private static ThemeTokenGroup GroupOf(string name)
    {
        foreach (var (match, text, group) in GroupRules)
        {
            if (Matches(name, match, text))
            {
                return group;
            }
        }

        return ThemeTokenGroup.Color;
    }

    private static bool Matches(string name, NameMatch match, string text) => match switch
    {
        NameMatch.Prefix => name.StartsWith(text, StringComparison.Ordinal),
        NameMatch.Suffix => name.EndsWith(text, StringComparison.Ordinal),
        NameMatch.Contains => name.Contains(text, StringComparison.Ordinal),
        _ => string.Equals(name, text, StringComparison.Ordinal)
    };

    /// <summary>How a rule of <see cref="GroupRules"/> compares its text with a token name.</summary>
    private enum NameMatch
    {
        Prefix,
        Suffix,
        Contains,
        Exact
    }

    /// <summary>
    /// The families in order of precedence: the first rule that matches wins, and a name no rule matches is a colour.
    /// </summary>
    private static readonly (NameMatch Match, string Text, ThemeTokenGroup Group)[] GroupRules =
    [
        (NameMatch.Prefix, "--omni-chart-", ThemeTokenGroup.Chart),
        (NameMatch.Prefix, "--omni-grid-", ThemeTokenGroup.Grid),
        (NameMatch.Prefix, "--omni-mindmap-", ThemeTokenGroup.Diagram),
        (NameMatch.Prefix, "--omni-space-", ThemeTokenGroup.Spacing),
        // How surfaces stand off the page: shadows, the floating layer and its acrylic, the overlay.
        (NameMatch.Prefix, "--omni-shadow", ThemeTokenGroup.Elevation),
        (NameMatch.Suffix, "-shadow", ThemeTokenGroup.Elevation),
        (NameMatch.Prefix, "--omni-layer-", ThemeTokenGroup.Elevation),
        (NameMatch.Prefix, "--omni-elevation-", ThemeTokenGroup.Elevation),
        (NameMatch.Prefix, "--omni-overlay-", ThemeTokenGroup.Elevation),
        (NameMatch.Exact, "--omni-focus-ring", ThemeTokenGroup.Elevation),
        (NameMatch.Exact, "--omni-focus-ring-danger", ThemeTokenGroup.Elevation),
        (NameMatch.Exact, "--omni-focus-ring-inset", ThemeTokenGroup.Elevation),
        (NameMatch.Exact, "--omni-color-overlay", ThemeTokenGroup.Elevation),
        (NameMatch.Exact, "--omni-scrim-filter", ThemeTokenGroup.Elevation),
        (NameMatch.Exact, "--omni-card-filter", ThemeTokenGroup.Elevation),
        (NameMatch.Prefix, "--omni-backdrop", ThemeTokenGroup.Elevation),
        (NameMatch.Prefix, "--omni-scope-backdrop-", ThemeTokenGroup.Elevation),
        (NameMatch.Exact, "--omni-scope-canvas", ThemeTokenGroup.Elevation),
        (NameMatch.Exact, "--omni-scope-page", ThemeTokenGroup.Elevation),
        // The stacking layers: how high a surface stands above the page.
        (NameMatch.Prefix, "--omni-z-", ThemeTokenGroup.Elevation),
        // How long a part takes to move, beside the movement of a pressed button.
        (NameMatch.Prefix, "--omni-duration-", ThemeTokenGroup.Shape),
        (NameMatch.Prefix, "--omni-color-", ThemeTokenGroup.Color),
        (NameMatch.Prefix, "--omni-font", ThemeTokenGroup.Typography),
        // How buttons and headings set their text: weight, case, tracking and the heading face.
        (NameMatch.Contains, "-font-", ThemeTokenGroup.Typography),
        (NameMatch.Suffix, "-text-transform", ThemeTokenGroup.Typography),
        (NameMatch.Suffix, "-letter-spacing", ThemeTokenGroup.Typography),
        // How parts are drawn: radii, borders, the card fill and the movement of a pressed button.
        (NameMatch.Prefix, "--omni-radius", ThemeTokenGroup.Shape),
        (NameMatch.Suffix, "-radius", ThemeTokenGroup.Shape),
        (NameMatch.Contains, "-border-", ThemeTokenGroup.Shape),
        (NameMatch.Suffix, "-transform", ThemeTokenGroup.Shape),
        (NameMatch.Exact, "--omni-border-width", ThemeTokenGroup.Shape),
        (NameMatch.Exact, "--omni-card-background", ThemeTokenGroup.Shape),
        (NameMatch.Exact, "--omni-dialog-background", ThemeTokenGroup.Shape),
        (NameMatch.Exact, "--omni-input-background", ThemeTokenGroup.Shape),
        (NameMatch.Exact, "--omni-alert-fill-opacity", ThemeTokenGroup.Shape),
        // The scope's own hooks: its stacking, the motion of its colour field, the fill of its shell.
        (NameMatch.Exact, "--omni-scope-isolation", ThemeTokenGroup.Shape),
        (NameMatch.Exact, "--omni-scope-motion", ThemeTokenGroup.Shape),
        (NameMatch.Exact, "--omni-shell-background", ThemeTokenGroup.Shape),
    ];

    // The brace before a selector is looked behind, not consumed: it closes the previous block, and a
    // minified stylesheet has no line start to anchor the next one on.
    [GeneratedRegex(@"(?<=^|\})\s*(?<selector>[^{}]*?)\s*\{(?<body>[^{}]*)\}", RegexOptions.Multiline)]
    private static partial Regex RootBlock();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex Comment();

    [GeneratedRegex(@"(?:^|;)\s*(?<name>--omni-[a-z0-9-]+)\s*:\s*(?<value>[^;]+)")]
    private static partial Regex Declaration();
}
