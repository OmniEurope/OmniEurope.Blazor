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
    /// Painted or drawn. <see cref="OmniFill.Solid"/>, the default, writes white on a fill of its tone
    /// deep enough for white to read at 4.5:1 in every theme; <see cref="OmniFill.Tonal"/> is the lighter tinted chip;
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
    /// content; an icon-only badge stays a square whatever the step. Whatever the step, a badge is never
    /// narrower than it is tall: a one-character badge ("1", "F") is a square, a longer text widens it.
    /// </summary>
    [Parameter]
    public OmniBadgeWidth Width { get; set; } = OmniBadgeWidth.Auto;

    /// <summary>
    /// The corners of the badge, whatever its <see cref="Fill"/>: <see cref="OmniBadgeShape.Pill"/>, the
    /// default, fully rounded ends; <see cref="OmniBadgeShape.Rounded"/>, the small radius of the theme (the
    /// look before the pill); <see cref="OmniBadgeShape.Square"/>, no radius. Colours, border and height are
    /// the same in the three (recette R-3).
    /// </summary>
    [Parameter]
    public OmniBadgeShape Shape { get; set; } = OmniBadgeShape.Pill;

    /// <summary>
    /// The lettering of a <see cref="OmniFill.Solid"/> badge: <see cref="OmniBadgeInk.White"/>, the
    /// default; <see cref="OmniBadgeInk.WhiteOutlined"/>, white with a 1 px black outline, for a badge laid
    /// over a picture or a busy colour; <see cref="OmniBadgeInk.Black"/>, black letters on a fill of the
    /// tone lightened for them. No effect on the tonal and outline fills.
    /// </summary>
    [Parameter]
    public OmniBadgeInk Ink { get; set; } = OmniBadgeInk.White;

    /// <summary>
    /// Makes the badge one real <c>&lt;button type="button"&gt;</c>, called when it is activated: a count
    /// that opens the list it counts, a tag that filters. It is focusable, Enter and Space activate it, and
    /// it shows a focus ring and a light hover mark (an underline and a tint, no lift or shadow) so that it
    /// does not read as an <see cref="OmniButton"/>. Cannot be combined with <see cref="Href"/>. Without
    /// either, the badge stays a plain <c>span</c>. The content should be a short text and an icon.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Makes the badge one real link (<c>a</c>) to this path or address (relative, http, https, mailto or tel;
    /// any other scheme is refused), like <see cref="OmniNavTile.Href"/>. It looks and focuses like the
    /// clickable badge of <see cref="OnClick"/>. Cannot be combined with <see cref="OnClick"/>. Null or blank,
    /// the default, leaves the badge a plain <c>span</c>.
    /// </summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>
    /// The accessible name of a clickable badge (<see cref="OnClick"/> or <see cref="Href"/>), for a badge
    /// whose text alone does not say what it does ("2" opens the list of the 2 users). Written as the
    /// <c>aria-label</c> attribute, over one passed by the host. A plain badge ignores it: its text stays its
    /// only name. Null, the default, leaves the badge named by its content.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    private string? _safeHref;

    /// <summary>Whether the badge is a button or a link instead of a plain span.</summary>
    private bool Clickable => OnClick.HasDelegate || _safeHref is not null;

    private string? AccessibleName => Clickable ? OmniAriaLabel.Of(Label, AdditionalAttributes) : null;

    private string BadgeClass => Css("omni-badge", $"omni-badge--{Tone.ToString().ToLowerInvariant()}", $"omni-badge--{Fill.ToString().ToLowerInvariant()}", IconOnly ? "omni-badge--icon" : null, LeadingIcon ? "omni-badge--leading-icon" : null, Clickable ? "omni-badge--action" : null, ShapeClass, InkClass, WidthClass);

    /// <summary>Checks that <see cref="Href"/> and <see cref="OnClick"/> are not both given and that the address is safe.</summary>
    /// <exception cref="ArgumentException"><see cref="Href"/> and <see cref="OnClick"/> are both given.</exception>
    /// <exception cref="InvalidOperationException"><see cref="Href"/> uses a scheme that is not allowed, or is a relative address that names another host (<c>//host</c>).</exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        var hasHref = !string.IsNullOrWhiteSpace(Href);
        if (hasHref && OnClick.HasDelegate)
        {
            throw new ArgumentException("OmniBadge takes either Href (a link) or OnClick (a button), not both.", nameof(Href));
        }

        _safeHref = hasHref ? OmniUriPolicy.EnsureSafe(Href, nameof(Href)) : null;
    }

    /// <summary>An icon and nothing else: the badge is then a square as tall as a text badge.</summary>
    private bool IconOnly => Icon is not null && ChildContent is null;

    /// <summary>An icon before a text: the start padding then gives back the margin drawn inside the glyph's box.</summary>
    private bool LeadingIcon => Icon is not null && ChildContent is not null;

    /// <summary>
    /// The text in its own box: the box whose line gets trimmed to the height of the capitals, so that the
    /// middle of the badge is the middle of the letters (the badge itself is a flex container, which the
    /// trim does not apply to).
    /// </summary>
    private RenderFragment? TextPart => ChildContent is null ? null : builder =>
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", "omni-badge__text");
        builder.AddContent(2, ChildContent);
        builder.CloseElement();
    };

    /// <summary>The pill, the default, is the base rule of the badge: only the other two shapes carry a modifier.</summary>
    private string? ShapeClass => Shape switch
    {
        OmniBadgeShape.Rounded => "omni-badge--rounded",
        OmniBadgeShape.Square => "omni-badge--square",
        _ => null,
    };

    private string? InkClass => Fill != OmniFill.Solid ? null : Ink switch
    {
        OmniBadgeInk.WhiteOutlined => "omni-badge--ink-white-outlined",
        OmniBadgeInk.Black => "omni-badge--ink-black",
        _ => null,
    };

    private string? WidthClass => Width == OmniBadgeWidth.Auto || IconOnly ? null : $"omni-badge--width-{Width.ToString().ToLowerInvariant()}";
}
