namespace OmniEurope.Blazor.Components;

/// <summary>One action of an <see cref="OmniOverflowMenu"/>: an icon and a label.</summary>
public partial class OmniOverflowMenuItem
{
    [CascadingParameter]
    private OmniOverflowMenu? Menu { get; set; }

    /// <summary>The label of the action.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The icon before the label, in general an <see cref="OmniIcon"/>; decorative. Without one the label keeps its column.</summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>Runs once the menu has closed.</summary>
    [Parameter]
    public EventCallback OnClick { get; set; }

    /// <summary>Disables the action: it stays listed, dimmed, and the arrows skip it.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>A destructive action (delete, revoke): its label and icon in the danger ink.</summary>
    [Parameter]
    public bool Danger { get; set; }

    private async Task ActivateAsync()
    {
        if (Disabled)
        {
            return;
        }

        if (Menu is not null)
        {
            await Menu.CloseAsync(restoreFocus: true);
        }

        await OnClick.InvokeAsync();
    }
}
