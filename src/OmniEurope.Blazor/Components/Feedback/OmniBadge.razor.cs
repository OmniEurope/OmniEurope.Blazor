namespace OmniEurope.Blazor.Components;

/// <summary>
/// A short label in a coloured chip: a status, a count, a tag. The text is its content; an
/// <see cref="Icon"/> may go before it, or stand alone.
/// </summary>
public partial class OmniBadge
{
    /// <summary>The text of the badge.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Icon drawn before the text, in general an <see cref="OmniIcon"/>, like the <c>Icon</c> of the other
    /// components. The badge sizes it to its own text; a size set on the icon wins. Without text, the
    /// badge is a square as tall as a text badge.
    /// </summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>The colour intention of the badge; <see cref="OmniTone.Neutral"/> by default.</summary>
    [Parameter]
    public OmniTone Tone { get; set; }

    /// <summary>
    /// Painted or drawn. <see cref="OmniFill.Solid"/>, the default, takes the fill and ink of the button
    /// of the same intention; <see cref="OmniFill.Tonal"/> is the lighter tinted chip;
    /// <see cref="OmniFill.Outline"/> is for a badge that has to read as a different kind of thing from
    /// the painted ones beside it, not merely as another colour.
    /// </summary>
    [Parameter]
    public OmniFill Fill { get; set; } = OmniFill.Solid;

    /// <summary>
    /// A common minimum width, so that badges of different text lengths in one column or list line up
    /// (recette R1-23): <see cref="OmniBadgeWidth.Small"/>, <see cref="OmniBadgeWidth.Medium"/> or
    /// <see cref="OmniBadgeWidth.Large"/>, the same step on every badge of the column. A longer text widens
    /// its badge, nothing is cut. <see cref="OmniBadgeWidth.Auto"/>, the default, keeps the width of the
    /// content; an icon-only badge stays a square whatever the step.
    /// </summary>
    [Parameter]
    public OmniBadgeWidth Width { get; set; } = OmniBadgeWidth.Auto;

    /// <summary>An icon and nothing else: the badge is then a square as tall as a text badge.</summary>
    private bool IconOnly => Icon is not null && ChildContent is null;

    private string? WidthClass => Width == OmniBadgeWidth.Auto || IconOnly ? null : $"omni-badge--width-{Width.ToString().ToLowerInvariant()}";
}
