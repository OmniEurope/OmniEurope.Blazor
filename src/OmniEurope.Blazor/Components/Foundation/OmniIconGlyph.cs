namespace OmniEurope.Blazor.Components;

/// <summary>
/// A single icon outline: SVG path data plus the square view box it was drawn on.
/// Consumers build one from any icon set they like, so the package never has to ship a catalogue.
/// </summary>
public sealed class OmniIconGlyph
{
    private const int PhosphorViewBoxSize = 256;

    public OmniIconGlyph(string pathData, int viewBoxSize)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pathData);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(viewBoxSize, 0);
        EnsurePathGrammar(pathData);

        PathData = pathData;
        ViewBoxSize = viewBoxSize;
    }

    /// <summary>The <c>d</c> attribute of the outline, expressed in view box units.</summary>
    public string PathData { get; }

    /// <summary>Side of the square view box the outline was drawn on.</summary>
    public int ViewBoxSize { get; }

    /// <summary>Builds a glyph on the 256 unit grid used by the Phosphor Icons set.</summary>
    public static OmniIconGlyph Phosphor(string pathData) => new(pathData, PhosphorViewBoxSize);

    internal string ViewBox => $"0 0 {ViewBoxSize} {ViewBoxSize}";

    // Path data reaches the DOM as an attribute value. Blazor already encodes it, so this guard is
    // not the last line of defence; it exists to reject markup that is not an outline at all.
    private static void EnsurePathGrammar(string pathData)
    {
        foreach (var character in pathData)
        {
            if (!IsPathCharacter(character))
            {
                throw new ArgumentException(
                    $"Character '{character}' is not valid SVG path data. A glyph carries an outline, not markup.",
                    nameof(pathData));
            }
        }
    }

    private static bool IsPathCharacter(char character) =>
        character is >= '0' and <= '9'
            || "MmLlHhVvCcSsQqTtAaZz".Contains(character, StringComparison.Ordinal)
            || character is 'e' or 'E'
            || character is '.' or ',' or '-' or '+'
            || char.IsWhiteSpace(character);
}
