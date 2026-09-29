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

    /// <summary>
    /// The accessible name of the button, needed when its content is an icon alone. Null, the default,
    /// leaves the button named by its content. An <c>aria-label</c> attribute is overridden by it.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// Draws a small dot in the danger fill at the button's top end corner, ringed by the surface: the
    /// mark of something waiting, such as unread notifications on a bell. The dot is decorative
    /// (<c>aria-hidden</c>), so what it signals belongs in the accessible name, for instance an
    /// <see cref="Label"/> of "Notifications, 3 unread". False, the default, draws nothing.
    /// </summary>
    [Parameter]
    public bool Indicator { get; set; }

    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    private Task HandleClickAsync(MouseEventArgs args) =>
        Disabled || Busy ? Task.CompletedTask : OnClick.InvokeAsync(args);
}
