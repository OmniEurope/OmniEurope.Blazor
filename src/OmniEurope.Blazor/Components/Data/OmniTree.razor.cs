namespace OmniEurope.Blazor.Components;

/// <summary>
/// A tree of <see cref="OmniTreeItem{TValue}"/> with single or multiple selection, bound through
/// <see cref="Value"/>.
/// </summary>
public partial class OmniTree<TValue>
{
    private OmniTreeContext<TValue> _context = default!;
    private IReadOnlyList<TValue>? _lastReceivedValues;

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

    /// <summary>The root <see cref="OmniTreeItem{TValue}"/> items.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private OmniTreeContext<TValue> Context => _context;

    protected override void OnInitialized() => _context = new OmniTreeContext<TValue> { ToggleSelectionAsync = ToggleSelectionAsync };

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
