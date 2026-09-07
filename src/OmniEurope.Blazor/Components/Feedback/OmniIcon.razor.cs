using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

public partial class OmniIcon
{
    [Parameter]
    public OmniIconName Name { get; set; }

    /// <summary>
    /// Outline to render instead of <see cref="Name"/>. Lets a consumer use any icon from any set
    /// without the package embedding that set.
    /// </summary>
    [Parameter]
    public OmniIconGlyph? Glyph { get; set; }

    [Parameter]
    public OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    [Parameter]
    public string? AriaLabel { get; set; }

    private OmniIconGlyph ResolvedGlyph => Glyph ?? PhosphorIconGlyphs.For(Name);
}
