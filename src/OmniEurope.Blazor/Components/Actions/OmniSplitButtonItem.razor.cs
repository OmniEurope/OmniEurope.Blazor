namespace OmniEurope.Blazor.Components;

public partial class OmniSplitButtonItem
{
    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public EventCallback OnClick { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private async Task HandleClickAsync()
    {
        if (!Disabled)
        {
            await OnClick.InvokeAsync();
        }
    }
}
