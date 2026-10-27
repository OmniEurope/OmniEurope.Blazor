using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>An outline the package does not embed, drawn through Glyph, and the file icons in their format colour.</summary>
public partial class IconGlyphExample
{
    private static readonly OmniControlSize[] Sizes = Enum.GetValues<OmniControlSize>();

    /// <summary>
    /// A Phosphor path the package does not embed: the proof that an icon of the whole set passes
    /// without the package carrying it.
    /// </summary>
    private static readonly OmniIconGlyph Compass = OmniIconGlyph.Phosphor(
        "M128,24A104,104,0,1,0,232,128,104.11,104.11,0,0,0,128,24Zm0,192a88,88,0,1,1,88-88A88.1,88.1,0,0,1,128,216Zm50.34-138.34a8,8,0,0,0-8.68-1.73l-56,24a8,8,0,0,0-4.2,4.2l-24,56a8,8,0,0,0,10.41,10.51l56-24a8,8,0,0,0,4.2-4.2l24-56A8,8,0,0,0,178.34,77.66ZM140,140l-33.42,14.32L120.9,120.9,154.32,106.6Z");
}
