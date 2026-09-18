namespace OmniEurope.Blazor.Components;

public partial class OmniProfileMenu
{
    private ElementReference _details;
    private OmniDisclosureDismissal? _dismissal;

    /// <summary>
    /// The accessible name of the trigger. Empty, the localized default ("Profile menu") is used.
    /// With the avatar trigger it is the only name the trigger has, so it should name the account.
    /// </summary>
    [Parameter]
    public string Label { get; set; } = string.Empty;

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("ProfileMenuLabel")
        : Label;

    /// <summary>
    /// What the closed menu shows. Null, the default, draws the avatar: a round disc holding
    /// <see cref="Initials"/>, or the user glyph without them.
    /// </summary>
    [Parameter]
    public RenderFragment? Summary { get; set; }

    /// <summary>
    /// A few letters (typically one to three) drawn in the avatar disc, of the trigger and of the <see cref="Header"/>,
    /// in place of the user glyph. Decorative: the trigger is named by <see cref="Label"/>.
    /// </summary>
    [Parameter]
    public string? Initials { get; set; }

    /// <summary>
    /// The identity shown at the top of the open menu, beside a large avatar disc: its first element
    /// reads as the name, the next ones as muted details (a role, an organisation, a link to the
    /// profile). Rendered outside the <c>role="menu"</c> list. Null, the default, renders no header.
    /// </summary>
    [Parameter]
    public RenderFragment? Header { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Closes the open menu when a press lands outside it, which is what a menu is expected to do.
    /// On by default; a page that drives the menu from controls of its own can turn it off, or mark
    /// those controls with <c>data-omni-keep-open</c> so a press on them never counts as outside.
    /// Escape and choosing an item close the menu either way.
    /// </summary>
    [Parameter]
    public bool CloseOnOutsideClick { get; set; } = true;

    /// <summary>The avatar disc's content: the initials, or the user glyph.</summary>
    private RenderFragment AvatarContent => builder =>
    {
        if (string.IsNullOrWhiteSpace(Initials))
        {
            builder.OpenComponent<OmniIcon>(0);
            builder.AddComponentParameter(1, nameof(OmniIcon.Name), OmniIconName.User);
            builder.CloseComponent();
        }
        else
        {
            builder.OpenElement(2, "span");
            builder.AddAttribute(3, "class", "omni-profile-menu__initials");
            builder.AddContent(4, Initials.Trim());
            builder.CloseElement();
        }
    };

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        _dismissal ??= new OmniDisclosureDismissal(JavaScript);
        return _dismissal.ApplyAsync(_details, CloseOnOutsideClick, closeOnItem: true);
    }

    public async ValueTask DisposeAsync()
    {
        if (_dismissal is not null)
        {
            await _dismissal.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// The density of this component and of what it holds (control heights, paddings, gaps), over
    /// the one it inherits from its theme scope or section. Null, the default, inherits it.
    /// </summary>
    [Parameter]
    public OmniDensity? Density { get; set; }
}
