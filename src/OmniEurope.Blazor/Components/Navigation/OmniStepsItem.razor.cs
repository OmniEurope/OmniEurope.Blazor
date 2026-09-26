namespace OmniEurope.Blazor.Components;

public partial class OmniStepsItem
{
    [CascadingParameter]
    private OmniStepsContext? Context { get; set; }

    [Parameter]
    public int Index { get; set; }

    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// An icon shown in the step's marker instead of its number, which stays readable by assistive
    /// technologies. Null, the default, shows the number.
    /// </summary>
    [Parameter]
    public OmniIconName? Icon { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Id of a panel rendered elsewhere that shows this step, as a wizard does with one body for all
    /// its steps. When set, the item renders no panel of its own and its button controls that one;
    /// null, the default, keeps the item's own panel.
    /// </summary>
    [Parameter]
    public string? PanelId { get; set; }

    private bool Selected => Context?.Value == Index;
    private string ButtonId => $"{Id ?? $"omni-step-{Index}"}-button";
    private string OwnPanelId => $"{Id ?? $"omni-step-{Index}"}-panel";
    private string ControlledPanelId => PanelId ?? OwnPanelId;
    private Task SelectAsync() => Disabled || Context is null ? Task.CompletedTask : Context.SelectAsync(Index);
}
