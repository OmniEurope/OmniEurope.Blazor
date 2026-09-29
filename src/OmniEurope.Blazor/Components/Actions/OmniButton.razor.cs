namespace OmniEurope.Blazor.Components;

/// <summary>
/// A <c>&lt;button&gt;</c> styled by its <see cref="Variant"/> and <see cref="Size"/>. Unknown
/// attributes are passed to the element, and the button's own class, type and state attributes win
/// over them.
/// </summary>
public partial class OmniButton
{
    /// <summary>The content of the button: its text, an icon, or both. Required.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>What the button means, which sets its colours. <see cref="OmniButtonVariant.Primary"/> by default.</summary>
    [Parameter]
    public OmniButtonVariant Variant { get; set; } = OmniButtonVariant.Primary;

    /// <summary>The height, padding and text size. <see cref="OmniControlSize.Medium"/> by default.</summary>
    [Parameter]
    public OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    /// <summary>
    /// The HTML <c>type</c> of the button. <see cref="OmniButtonType.Button"/> by default, so a button
    /// inside a form does not submit it unless asked to.
    /// </summary>
    [Parameter]
    public OmniButtonType ButtonType { get; set; } = OmniButtonType.Button;

    /// <summary>Disables the button: it renders the <c>disabled</c> attribute and does not raise <see cref="OnClick"/>.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Marks the button as working (<c>aria-busy</c> and a busy style) without disabling it, so its size
    /// and colours do not change. While busy, <see cref="OnClick"/> is not raised and the click's default
    /// action is prevented, so pressing Enter does not submit the form again.
    /// </summary>
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

    /// <summary>Raised when the button is clicked, unless it is <see cref="Disabled"/> or <see cref="Busy"/>.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    private Task HandleClickAsync(MouseEventArgs args) =>
        Disabled || Busy ? Task.CompletedTask : OnClick.InvokeAsync(args);
}
