using System.Collections.Frozen;
using System.Text;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

// Built-in outlines, traced from the Phosphor Icons "regular" set (MIT).
// Attribution and licence text live in docs/third-party-licenses and eng/vendored-assets.json.
// A chosen catalogue, not the full set: the glyphs the package renders itself plus every icon its
// applications display, one per OmniIconName value. The outlines live in PhosphorIcons.txt, embedded
// as UTF-8 and read once, on the first icon rendered: as C# string literals they cost two bytes a
// character. An application that needs a new icon adds its name there; OmniIcon.Glyph stays
// available for a one-off outline.
internal static class PhosphorIconGlyphs
{
    private const string ResourceName = "OmniEurope.Blazor.PhosphorIcons.txt";

    private static readonly Lazy<FrozenDictionary<OmniIconName, OmniIconGlyph>> Catalog = new(Load);

    /// <summary>The built-in outline of an icon name; a value without one throws.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The name has no built-in outline.</exception>
    internal static OmniIconGlyph For(OmniIconName name) =>
        Catalog.Value.TryGetValue(name, out var glyph)
            ? glyph
            : throw new ArgumentOutOfRangeException(nameof(name), name, "The icon name has no glyph.");

    // One line per icon: the OmniIconName member, a tab, then the path data on the 256 unit grid.
    private static FrozenDictionary<OmniIconName, OmniIconGlyph> Load()
    {
        using var resource = typeof(PhosphorIconGlyphs).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The icon catalogue resource {ResourceName} is missing from the assembly.");
        using var reader = new StreamReader(resource, Encoding.UTF8);

        var glyphs = new Dictionary<OmniIconName, OmniIconGlyph>();
        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var tab = line.IndexOf('\t', StringComparison.Ordinal);
            glyphs.Add(Enum.Parse<OmniIconName>(line.AsSpan(0, tab)), OmniIconGlyph.Phosphor(line[(tab + 1)..]));
        }

        return glyphs.ToFrozenDictionary();
    }
}
