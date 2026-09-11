namespace OmniEurope.Blazor.Components;

/// <summary>
/// The action bar of an <see cref="OmniMindMap"/>: add a node, delete the selection, duplicate,
/// draw a link, centre on the selected node, reorganize the map and fit it in the canvas.
/// </summary>
/// <remarks>
/// Place it in the map's <see cref="OmniMindMap.ToolbarContent"/>. Its labels are the map's
/// <see cref="OmniMindMap.ContextLabels"/>, so the toolbar and the context menu always name an
/// action the same way. A read-only map keeps only centring and fitting.
/// </remarks>
public partial class OmniMindMapToolbar : IDisposable
{
    private OmniMindMap? _subscribed;

    /// <summary>Accessible name of the action group. Defaults to the localized "mind map actions".</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    [CascadingParameter]
    private OmniMindMap? Owner { get; set; }

    private OmniMindMap Map => Owner
        ?? throw new InvalidOperationException($"{nameof(OmniMindMapToolbar)} must be placed inside the ToolbarContent of an {nameof(OmniMindMap)}.");

    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel) ? Localize("MindMapToolbarLabel") : AriaLabel;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        var map = Map;
        if (!ReferenceEquals(_subscribed, map))
        {
            Unsubscribe();
            _subscribed = map;
            map.StateChanged += OnMapStateChanged;
        }
    }

    public void Dispose()
    {
        Unsubscribe();
        GC.SuppressFinalize(this);
    }

    private void Unsubscribe()
    {
        if (_subscribed is not null)
        {
            _subscribed.StateChanged -= OnMapStateChanged;
            _subscribed = null;
        }
    }

    private void OnMapStateChanged() => _ = InvokeAsync(StateHasChanged);

    private Task AddNodeAsync() => Map.DispatchAsync(Map.AddNodeAsync);

    private Task DeleteAsync() => Map.DispatchAsync(Map.DeleteSelectionAsync);

    private Task DuplicateAsync() => Map.DispatchAsync(Map.DuplicateSelectionAsync);

    private Task LinkAsync() => Map.DispatchAsync(() => Map.StartLinkModeAsync());

    private Task CenterAsync() => Map.DispatchAsync(Map.CenterOnSelectionAsync);

    private Task AutoLayoutAsync() => Map.DispatchAsync(Map.AutoLayoutAsync);

    private Task FitViewAsync() => Map.DispatchAsync(async () => await Map.FitViewAsync());
}
