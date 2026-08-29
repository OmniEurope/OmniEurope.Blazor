namespace OmniEurope.Blazor.Components;

public partial class OmniTabsItem
{
    [CascadingParameter]
    private OmniTabsContext? Context { get; set; }

    [Parameter, EditorRequired]
    public string Key { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Rendered before the title. A slot rather than a name, so the consumer keeps its own icon set.
    /// </summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private bool Selected => Context?.Value == Key;

    // The tabs render their content once per phase; this instance emits only the half it is asked
    // for, so the button lives in the scrolling strip and the panel below it.
    private bool IsPanelPhase => Context?.Phase == OmniTabsPhase.Panel;
    private string TabId => $"{Id ?? $"omni-tab-{Key}"}-tab";
    private string PanelId => $"{Id ?? $"omni-tab-{Key}"}-panel";
    private Task SelectAsync() => Disabled || Context is null ? Task.CompletedTask : Context.SelectAsync(Key);
}
