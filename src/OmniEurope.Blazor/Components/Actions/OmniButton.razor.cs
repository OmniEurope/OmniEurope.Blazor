namespace OmniEurope.Blazor.Components;

public partial class OmniButton
{
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public OmniButtonVariant Variant { get; set; } = OmniButtonVariant.Primary;

    [Parameter]
    public OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    [Parameter]
    public OmniButtonType ButtonType { get; set; } = OmniButtonType.Button;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool Busy { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Draws a small dot in the danger fill at the button's top end corner, ringed by the surface: the
    /// mark of something waiting, such as unread notifications on a bell. The dot is decorative
    /// (<c>aria-hidden</c>), so what it signals belongs in the accessible name, for instance an
    /// <see cref="AriaLabel"/> of "Notifications, 3 unread". False, the default, draws nothing.
    /// </summary>
    [Parameter]
    public bool Indicator { get; set; }

    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    private Task HandleClickAsync(MouseEventArgs args) =>
        Disabled || Busy ? Task.CompletedTask : OnClick.InvokeAsync(args);

    /// <summary>
    /// The density of this component and of what it holds (control heights, paddings, gaps), over
    /// the one it inherits from its theme scope or section. Null, the default, inherits it.
    /// </summary>
    [Parameter]
    public OmniDensity? Density { get; set; }
}
