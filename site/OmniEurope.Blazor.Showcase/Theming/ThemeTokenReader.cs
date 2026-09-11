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
    /// Extracts the declarations of the first <c>:root</c> block, in the order the file declares
    /// them so the editor presents them the way the stylesheet is written.
    /// </summary>
    public static IReadOnlyList<ThemeToken> Parse(string css)
    {
        var block = RootBlock().Match(css);
        if (!block.Success)
        {
            return [];
        }

        // Release ships the stylesheet minified on one line, so nothing here may depend on line breaks.
        var body = Comment().Replace(block.Groups["body"].Value, string.Empty);
        var tokens = new List<ThemeToken>();
        foreach (Match declaration in Declaration().Matches(body))
        {
            var name = declaration.Groups["name"].Value;
            var value = declaration.Groups["value"].Value.Trim();
            tokens.Add(new ThemeToken(name, value, GroupOf(name)));
        }

        return tokens;
    }

    private static ThemeTokenGroup GroupOf(string name) => name switch
    {
        _ when name.StartsWith("--omni-chart-", StringComparison.Ordinal) => ThemeTokenGroup.Chart,
        _ when name.StartsWith("--omni-grid-", StringComparison.Ordinal) => ThemeTokenGroup.Grid,
        _ when name.StartsWith("--omni-shadow", StringComparison.Ordinal) => ThemeTokenGroup.Elevation,
        "--omni-focus-ring" or "--omni-color-overlay" => ThemeTokenGroup.Elevation,
        _ when name.StartsWith("--omni-color-", StringComparison.Ordinal) => ThemeTokenGroup.Color,
        _ when name.StartsWith("--omni-font", StringComparison.Ordinal) => ThemeTokenGroup.Typography,
        _ when name.StartsWith("--omni-space-", StringComparison.Ordinal) => ThemeTokenGroup.Spacing,
        "--omni-control-height" => ThemeTokenGroup.Spacing,
        _ when name.StartsWith("--omni-radius", StringComparison.Ordinal) => ThemeTokenGroup.Shape,
        "--omni-border-width" => ThemeTokenGroup.Shape,
        _ => ThemeTokenGroup.Color
    };

    [GeneratedRegex(@"(?:^|\})\s*:root\s*\{(?<body>[^}]*)\}", RegexOptions.Multiline)]
    private static partial Regex RootBlock();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex Comment();

    [GeneratedRegex(@"(?:^|;)\s*(?<name>--omni-[a-z0-9-]+)\s*:\s*(?<value>[^;]+)")]
    private static partial Regex Declaration();
}
