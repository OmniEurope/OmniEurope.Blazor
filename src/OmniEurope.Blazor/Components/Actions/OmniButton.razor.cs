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

    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    private Task HandleClickAsync(MouseEventArgs args) =>
        Disabled || Busy ? Task.CompletedTask : OnClick.InvokeAsync(args);
}
