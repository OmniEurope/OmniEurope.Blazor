using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// An inline SVG icon that takes the current text colour: a built-in outline chosen by
/// <see cref="Name"/>, or any outline passed as <see cref="Glyph"/>.
/// </summary>
public partial class OmniIcon
{
    /// <summary>
    /// The built-in icon to draw; <see cref="OmniIconName.Check"/> by default. Ignored when
    /// <see cref="Glyph"/> is set. A value outside <see cref="OmniIconName"/> throws
    /// <see cref="ArgumentOutOfRangeException"/> when the icon renders, rather than drawing another icon.
    /// </summary>
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

    /// <summary>
    /// Accessible name of the icon, also shown as its tooltip. Null by default: the icon is decorative
    /// and hidden from assistive technologies (its meaning comes from the text next to it).
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    private OmniIconGlyph ResolvedGlyph => Glyph ?? PhosphorIconGlyphs.For(Name);

    private string? TooltipText => !string.IsNullOrWhiteSpace(Label)
        ? Label
        : AdditionalAttributes is not null && AdditionalAttributes.TryGetValue("title", out var title) ? title?.ToString() : null;
}
