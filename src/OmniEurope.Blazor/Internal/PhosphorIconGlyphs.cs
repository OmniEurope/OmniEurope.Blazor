using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

// Built-in outlines, traced from the Phosphor Icons "regular" set (MIT).
// Attribution and licence text live in docs/third-party-licenses and eng/vendored-assets.json.
// A chosen catalogue, not the full set: the glyphs the package renders itself plus every icon its
// applications display, one per OmniIconName value. An application that needs a new icon adds its
// name and outline to the class of its alphabetical range (PhosphorIconGlyphsAToC,
// PhosphorIconGlyphsDToM or PhosphorIconGlyphsNToZ); OmniIcon.Glyph stays available for a one-off outline.
internal static class PhosphorIconGlyphs
{
    /// <summary>The built-in outline of an icon name; a value without one throws.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The name has no built-in outline.</exception>
    internal static OmniIconGlyph For(OmniIconName name) =>
        PhosphorIconGlyphsAToC.Find(name)
        ?? PhosphorIconGlyphsDToM.Find(name)
        ?? PhosphorIconGlyphsNToZ.Find(name)
        ?? throw new ArgumentOutOfRangeException(nameof(name), name, "The icon name has no glyph.");
}
