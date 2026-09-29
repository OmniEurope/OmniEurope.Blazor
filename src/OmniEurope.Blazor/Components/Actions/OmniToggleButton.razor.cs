namespace OmniEurope.Blazor.Components;

/// <summary>
/// A button that stays pressed or released, rendered with <c>aria-pressed</c>. It is controlled: a click
/// raises <see cref="ValueChanged"/> with the opposite state, and the button shows the new state only
/// once the host passes it back through <see cref="Value"/> (bind it with <c>@bind-Value</c>).
/// </summary>
public partial class OmniToggleButton
{
    /// <summary>True when the button is pressed. False by default.</summary>
    [Parameter]
    public bool Value { get; set; }

    /// <summary>
    /// Raised on a click with the opposite of <see cref="Value"/>, unless the button is
    /// <see cref="Disabled"/> or <see cref="Busy"/>.
    /// </summary>
    [Parameter]
    public EventCallback<bool> ValueChanged { get; set; }

    /// <summary>The same three sizes as <see cref="OmniButton"/>; with an icon alone the button is square.</summary>
    [Parameter]
    public OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    /// <summary>Disables the button: it renders the <c>disabled</c> attribute and does not raise <see cref="ValueChanged"/>.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Marks the button as working (<c>aria-busy</c> and a busy style) without disabling it. While busy,
    /// a click does not raise <see cref="ValueChanged"/>.
    /// </summary>
    [Parameter]
    public bool Busy { get; set; }

    /// <summary>The content of the button: its text, an icon, or both.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The accessible name, needed when the content is an icon alone. Null, the default, leaves the
    /// button named by its content. It wins over an <c>aria-label</c> passed as an attribute.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    private Task ToggleAsync() => Disabled || Busy ? Task.CompletedTask : ValueChanged.InvokeAsync(!Value);
}
