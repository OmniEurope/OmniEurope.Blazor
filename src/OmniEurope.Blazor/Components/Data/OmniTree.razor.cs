using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A tree of <see cref="OmniTreeItem{TValue}"/> with single or multiple selection, bound through
/// <see cref="Value"/>, whose items can be dragged onto one another (<see cref="AllowDragDrop"/>).
/// </summary>
/// <typeparam name="TValue">The type of the values the items stand for, compared with the default equality.</typeparam>
public partial class OmniTree<TValue>
{
    private OmniTreeContext<TValue> _context = default!;
    private IReadOnlyList<TValue>? _lastReceivedValues;
    private ElementReference _root;
    private IJSObjectReference? _module;

    /// <summary>
    /// The values of the selected items (<c>@bind-Value</c>), empty when none is. The tree keeps its own
    /// selection and reports each change through <see cref="ValueChanged"/>; a new list from the host
    /// replaces it.
    /// </summary>
    [Parameter]
    public IReadOnlyList<TValue> Value { get; set; } = Array.Empty<TValue>();

    /// <summary>Raised with the selected values each time an item is selected or unselected.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<TValue>> ValueChanged { get; set; }

    /// <summary>Lets several items be selected at once; off, selecting an item unselects the other.</summary>
    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>Accessible name of the tree; null uses the localized "tree".</summary>
    [Parameter]
    public string? Label { get; set; }

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("TreeLabel")
        : Label;

    /// <summary>
    /// Lets the reader drag an item onto another with the pointer (HTML drag and drop): the tree reports the
    /// pair through <see cref="OnItemDropped"/> and the host moves the item in its data. An item cannot be
    /// dropped onto itself nor onto one of its own descendants. There is no keyboard equivalent: a host that
    /// needs one offers a "move to" action of its own. Off by default.
    /// </summary>
    [Parameter]
    public bool AllowDragDrop { get; set; }

    /// <summary>Whether an item can be dragged; null lets every enabled item be.</summary>
    [Parameter]
    public Func<TValue, bool>? CanDrag { get; set; }

    /// <summary>
    /// Whether the dragged item (first value) can be dropped onto the target item (second value); null accepts
    /// every target that is neither the item nor one of its descendants. A target that refuses gets no drop
    /// highlight and the browser shows the drop as not allowed.
    /// </summary>
    [Parameter]
    public Func<TValue, TValue, bool>? CanDrop { get; set; }

    /// <summary>Raised when an item is dropped onto an item that accepts it, with both values.</summary>
    [Parameter]
    public EventCallback<OmniTreeDropEventArgs<TValue>> OnItemDropped { get; set; }

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    /// <summary>The root <see cref="OmniTreeItem{TValue}"/> items.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private OmniTreeContext<TValue> Context => _context;

    /// <summary>Creates the context the items of this tree share to read and toggle the selection.</summary>
    protected override void OnInitialized() => _context = new OmniTreeContext<TValue> { ToggleSelectionAsync = ToggleSelectionAsync, Tree = this };

    /// <summary>
    /// Opens every item that has children, and the items they reveal as they render, each reporting it
    /// through its <c>ExpandedChanged</c>; branches that load their children on demand load them.
    /// </summary>
    /// <returns>A task that completes when the items rendered so far are open.</returns>
    public Task ExpandAllAsync() => SetAllExpandedAsync(true);

    /// <summary>Closes every item, each reporting it through its <c>ExpandedChanged</c>.</summary>
    /// <returns>A task that completes when the items are closed.</returns>
    public Task CollapseAllAsync() => SetAllExpandedAsync(false);

    private async Task SetAllExpandedAsync(bool expanded)
    {
        _context.ExpandAll = expanded;
        foreach (var item in _context.Items.ToArray())
        {
            await item.SetExpandedAsync(expanded);
        }
    }

    internal void Redraw() => _ = InvokeAsync(StateHasChanged);

    /// <summary>
    /// Gives each drag the data some browsers need before they start one: the script only fills the drag in,
    /// .NET keeps the state.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (AllowDragDrop && _module is null)
        {
            _module = await JavaScript.InvokeAsync<IJSObjectReference>("import", Internal.OmniModules.Tree);
            await _module.InvokeVoidAsync("attachTreeDrag", _root);
        }
    }

    /// <summary>Detaches the drag script.</summary>
    /// <returns>A task that completes when the script is released.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("detachTreeDrag", _root);
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the script with it.
        }
    }

    /// <summary>Replaces the tree's selection with <see cref="Value"/> when the host passes another list instance.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!ReferenceEquals(_lastReceivedValues, Value))
        {
            _context.SelectedValues = Value;
            _lastReceivedValues = Value;
        }
    }

    private Task ToggleSelectionAsync(TValue value)
    {
        var selected = _context.SelectedValues.ToList();
        var existing = selected.FindIndex(item => EqualityComparer<TValue>.Default.Equals(item, value));
        if (existing >= 0)
        {
            selected.RemoveAt(existing);
        }
        else
        {
            if (!Multiple)
            {
                selected.Clear();
            }
            selected.Add(value);
        }

        _context.SelectedValues = selected;
        return ValueChanged.InvokeAsync(selected);
    }
}
