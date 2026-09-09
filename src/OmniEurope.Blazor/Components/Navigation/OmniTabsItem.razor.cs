using Microsoft.AspNetCore.Components.Web;

namespace OmniEurope.Blazor.Components;

public partial class OmniTabsItem
{
    private bool _visited;

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

    /// <summary>
    /// Replaces the title text inside the tab. The tab is a button, so the fragment has to stay
    /// non-interactive content (a count, a badge, a marker); <see cref="Title"/> is still required
    /// and becomes the tab's accessible name.
    /// </summary>
    [Parameter]
    public RenderFragment? TitleContent { get; set; }

    /// <summary>
    /// Raised when something is dropped onto the tab. Declared here rather than splatted, because
    /// the CSP contract refuses an <c>on*</c> attribute passed through the component's attributes.
    /// Setting it is what makes the tab a drop target: dragover is then cancelled for you, which is
    /// what tells the browser the drop is allowed.
    /// </summary>
    [Parameter]
    public EventCallback<DragEventArgs> OnDrop { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private bool Selected => Context?.Value == Key;

    protected override void OnParametersSet() => _visited |= Selected;

    // The tabs render their content once per phase; this instance emits only the half it is asked
    // for, so the button lives in the scrolling strip and the panel below it.
    private bool IsPanelPhase => Context?.Phase == OmniTabsPhase.Panel;
    private string TabId => $"{Id ?? $"omni-tab-{Key}"}-tab";
    private string PanelId => $"{Id ?? $"omni-tab-{Key}"}-panel";
    private Task SelectAsync() => Disabled || Context is null ? Task.CompletedTask : Context.SelectAsync(Key);

    private bool AcceptsDrop => OnDrop.HasDelegate;

    private Task HandleDropAsync(DragEventArgs args) => OnDrop.InvokeAsync(args);

    private RenderFragment DefaultTitle => builder => builder.AddContent(0, Title);
}
