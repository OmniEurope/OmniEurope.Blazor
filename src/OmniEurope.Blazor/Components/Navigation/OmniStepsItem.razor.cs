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

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private bool Selected => Context?.Value == Index;
    private string ButtonId => $"{Id ?? $"omni-step-{Index}"}-button";
    private string PanelId => $"{Id ?? $"omni-step-{Index}"}-panel";
    private Task SelectAsync() => Disabled || Context is null ? Task.CompletedTask : Context.SelectAsync(Index);
}
