namespace OmniEurope.Blazor.Components;

/// <summary>
/// One item of a menu: of <see cref="OmniOverflowMenu"/>, <see cref="OmniContextMenu"/>,
/// <see cref="OmniSplitButton"/> or <see cref="OmniProfileMenu"/>. A <c>role="menuitem"</c> reached by
/// the arrows of its menu (never by Tab), drawn as an icon column and a label. A button, or a link
/// with <see cref="Href"/>; choosing it closes the menu, the focus back on its trigger, then raises
/// <see cref="OnClick"/>.
/// </summary>
public partial class OmniMenuItem
{
    [CascadingParameter]
    private IOmniMenu? Menu { get; set; }

    /// <summary>The label of the item.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The icon before the label, in general an <see cref="OmniIcon"/>; decorative, the label names the
    /// item. Without one the label keeps its column, so the labels of a menu stay aligned.
    /// </summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>
    /// Makes the item a link to this address, checked by the package's URI policy (an unsafe scheme
    /// throws). Null, the default, draws a button. A disabled item is always a button.
    /// </summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>Disables the item: it stays listed, dimmed, the arrows skip it and choosing it does nothing.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// The colour intention of the label and icon. <see cref="OmniTone.Neutral"/>, the default, keeps
    /// the text colour; <see cref="OmniTone.Danger"/> marks a destructive action (delete, revoke).
    /// </summary>
    [Parameter]
    public OmniTone Tone { get; set; } = OmniTone.Neutral;

    /// <summary>Raised once the menu has closed, with the click that chose the item.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    private string ItemCss => Css(
        "omni-menu__item",
        Tone == OmniTone.Neutral ? null : $"omni-menu__item--{Tone.ToString().ToLowerInvariant()}");

    private string? SafeHref => OmniUriPolicy.EnsureSafe(Href, nameof(Href));

    private async Task ActivateAsync(MouseEventArgs args)
    {
        if (Disabled)
        {
            return;
        }

        if (Menu is not null)
        {
            await Menu.CloseAsync(restoreFocus: true);
        }

        await OnClick.InvokeAsync(args);
    }
}
