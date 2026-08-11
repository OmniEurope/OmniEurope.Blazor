namespace OmniEurope.Blazor.Components;

public partial class OmniTree<TValue>
{
    private OmniTreeContext<TValue> _context = default!;
    private IReadOnlyList<TValue>? _lastReceivedValues;

    [Parameter]
    public IReadOnlyList<TValue> SelectedValues { get; set; } = Array.Empty<TValue>();

    [Parameter]
    public EventCallback<IReadOnlyList<TValue>> SelectedValuesChanged { get; set; }

    [Parameter]
    public bool Multiple { get; set; }

    [Parameter]
    public string Label { get; set; } = string.Empty;

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("TreeLabel")
        : Label;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private OmniTreeContext<TValue> Context => _context;

    protected override void OnInitialized() => _context = new OmniTreeContext<TValue> { ToggleSelectionAsync = ToggleSelectionAsync };

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!ReferenceEquals(_lastReceivedValues, SelectedValues))
        {
            _context.SelectedValues = SelectedValues;
            _lastReceivedValues = SelectedValues;
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
        return SelectedValuesChanged.InvokeAsync(selected);
    }
}
