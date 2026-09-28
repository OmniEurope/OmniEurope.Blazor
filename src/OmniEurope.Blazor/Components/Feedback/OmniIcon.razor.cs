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

    /// <summary>
    /// Fixed size of the glyph. Null by default: the icon takes the size its container gives its icons
    /// (a badge, a split button, a small button), and the medium size elsewhere. A size set here, or a
    /// class of the consumer that sizes the icon, wins over the container.
    /// </summary>
    [Parameter]
    public OmniControlSize? Size { get; set; }

    private string? SizeClass => Size is { } size ? $"omni-icon--{size.ToString().ToLowerInvariant()}" : null;

    [Parameter]
    public string? AriaLabel { get; set; }

    private OmniIconGlyph ResolvedGlyph => Glyph ?? PhosphorIconGlyphs.For(Name);

    private string? TooltipText => !string.IsNullOrWhiteSpace(AriaLabel)
        ? AriaLabel
        : AdditionalAttributes is not null && AdditionalAttributes.TryGetValue("title", out var title) ? title?.ToString() : null;
}
