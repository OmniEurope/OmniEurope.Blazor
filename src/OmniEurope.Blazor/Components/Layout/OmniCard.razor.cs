namespace OmniEurope.Blazor.Components;

/// <summary>
/// A bordered surface (<c>section</c>) that groups related content, with an optional header and footer.
/// Given <see cref="Href"/> or <see cref="OnClick"/>, the whole card becomes one link or one button, and
/// keeps its place in an <see cref="OmniTileGrid"/> (same width and height as its neighbours, a
/// <see cref="DoubleWidth"/> card spans two columns).
/// </summary>
/// <remarks>
/// A clickable card has one root element that is the interactive one, so it is focusable, answers Enter
/// (and Space for the button) and shows the focus ring. The content stays inside that root, which fixes
/// what it may hold. With <see cref="Href"/> the root is an <c>a</c>, which accepts flow content: the
/// header, body and footer are divisions and headings placed in them stay exposed to assistive technology.
/// With <see cref="OnClick"/> the root is a <c>button</c>, which accepts phrasing content only: the three
/// parts are spans drawn as blocks, so <see cref="Header"/>, <see cref="ChildContent"/> and
/// <see cref="Footer"/> must hold phrasing content (text, <c>span</c>, <see cref="OmniIcon"/>), not
/// headings, paragraphs or divisions, and a button exposes its content as its accessible name rather than
/// as separate elements. Give an <c>aria-label</c> as an additional attribute to shorten that name. Never
/// put an interactive element (button, link, field) inside a clickable card: nesting it is invalid and the
/// component does not detect it. In a clickable card the body is clipped instead of scrolled, because a
/// scrolling region inside a link or a button is a nested focus stop; the card is the summary, the
/// detail is behind the click.
/// </remarks>
public partial class OmniCard
{
    private string? _safeHref;

    /// <summary>The top band of the card (<c>header</c>); null, the default, renders none.</summary>
    [Parameter]
    public RenderFragment? Header { get; set; }

    /// <summary>The body of the card. Required.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The bottom band of the card (<c>footer</c>), for its actions; null, the default, renders none.</summary>
    [Parameter]
    public RenderFragment? Footer { get; set; }

    /// <summary>
    /// Makes the whole card one link to this path or address (relative, http, https, mailto or tel; any other
    /// scheme is refused), like <see cref="OmniNavTile.Href"/>. Null, the default, leaves the card a plain
    /// <c>section</c>. Cannot be combined with <see cref="OnClick"/>. The content must hold no interactive
    /// element (nested interactive content is invalid and is not detected).
    /// </summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>
    /// Makes the whole card one button, called when it is activated. Cannot be combined with
    /// <see cref="Href"/>. The content must be phrasing content and hold no interactive element (invalid
    /// nesting, not detected); see the remarks of the component.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// In an <see cref="OmniTileGrid"/>, the card spans two columns instead of one. Ignored elsewhere.
    /// False by default.
    /// </summary>
    [Parameter]
    public bool DoubleWidth { get; set; }

    /// <summary>
    /// The density of this component and of what it holds (control heights, paddings, gaps), over
    /// the one it inherits from its theme scope or section. Null, the default, inherits it.
    /// </summary>
    [Parameter]
    public OmniDensity? Density { get; set; }

    /// <summary>Checks that <see cref="Href"/> and <see cref="OnClick"/> are not both given and that the address is safe.</summary>
    /// <exception cref="ArgumentException"><see cref="Href"/> and <see cref="OnClick"/> are both given.</exception>
    /// <exception cref="InvalidOperationException"><see cref="Href"/> uses a scheme that is not allowed.</exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        var hasHref = !string.IsNullOrWhiteSpace(Href);
        if (hasHref && OnClick.HasDelegate)
        {
            throw new ArgumentException("OmniCard takes either Href (a link) or OnClick (a button), not both.", nameof(Href));
        }

        _safeHref = hasHref ? OmniUriPolicy.EnsureSafe(Href, nameof(Href)) : null;
    }
}
