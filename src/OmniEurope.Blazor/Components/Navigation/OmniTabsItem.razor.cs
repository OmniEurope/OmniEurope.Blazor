using Microsoft.AspNetCore.Components.Web;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// One tab of <see cref="OmniTabs"/>: its button in the strip and its panel, built on first selection
/// and kept afterwards.
/// </summary>
public partial class OmniTabsItem
{
    private int _dragDepth;
    private bool _visited;
    private bool _wasSelected;

    [CascadingParameter]
    private OmniTabsContext? Context { get; set; }

    /// <summary>The stable key of the tab, the value of <see cref="OmniTabs.Value"/>. Null, the default, uses <see cref="Title"/>.</summary>
    [Parameter]
    public string? Key { get; set; }

    /// <summary>The title of the tab, its accessible name.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Rendered before the title. A slot rather than a name, so the consumer keeps its own icon set.
    /// </summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>Disables the tab: it cannot be selected and the arrows skip it.</summary>
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

    /// <summary>The content of the panel.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string EffectiveKey => string.IsNullOrWhiteSpace(Key) ? Title : Key;
    private string RegisteredKey => Context?.RegisterKey(EffectiveKey, Disabled) ?? EffectiveKey;
    private bool Selected => Context?.Value == RegisteredKey;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        _visited |= Selected || Context?.RenderAllPanels == true;
    }

    // Retain inactive panels without rebuilding their grids on every click. The latest parameters
    // are rendered on selection; one final render on deselection applies the hidden attribute.
    protected override bool ShouldRender() => !IsPanelPhase || Selected || _wasSelected;

    protected override void OnAfterRender(bool firstRender) => _wasSelected = Selected;

    // The tabs render their content once per phase; this instance emits only the half it is asked
    // for, so the button lives in the scrolling strip and the panel below it.
    private bool IsPanelPhase => Context?.Phase == OmniTabsPhase.Panel;
    private string TabId => $"{SafeDomId(Id ?? $"{Context?.IdPrefix ?? "omni-tab"}-{RegisteredKey}")}-tab";
    private string PanelId => $"{SafeDomId(Id ?? $"{Context?.IdPrefix ?? "omni-tab"}-{RegisteredKey}")}-panel";
    private Task SelectAsync() => Disabled || Context is null ? Task.CompletedTask : Context.SelectAsync(RegisteredKey);

    private bool AcceptsDrop => OnDrop.HasDelegate;

    // A drag entering a child of the tab leaves the tab first: the depth keeps the feedback on until
    // the drag leaves the tab itself.
    private bool DragOver => _dragDepth > 0;

    private string TabCss => CssClassBuilder.Combine([
        "omni-tabs__tab",
        AcceptsDrop ? "omni-tabs__tab--drop-target" : null,
        AcceptsDrop && DragOver ? "omni-tabs__tab--drag-over" : null]);

    private EventCallback<DragEventArgs> DragEnterCallback => AcceptsDrop
        ? EventCallback.Factory.Create<DragEventArgs>(this, () => _dragDepth++)
        : default;

    private EventCallback<DragEventArgs> DragLeaveCallback => AcceptsDrop
        ? EventCallback.Factory.Create<DragEventArgs>(this, () => _dragDepth = Math.Max(0, _dragDepth - 1))
        : default;

    private Task HandleDropAsync(DragEventArgs args)
    {
        _dragDepth = 0;
        return OnDrop.InvokeAsync(args);
    }

    private RenderFragment DefaultTitle => builder => builder.AddContent(0, Title);

    private static string SafeDomId(string value) => string.Concat(value.Select(character =>
        char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or ':' or '.'
            ? character.ToString()
            : $"-{(int)character:x}-"));
}
