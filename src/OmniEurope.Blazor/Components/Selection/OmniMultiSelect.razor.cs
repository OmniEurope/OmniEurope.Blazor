using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Components;

public partial class OmniMultiSelect<TValue>
{
    private string? _filter;
    private string? _boundFilterText;

    [Parameter, EditorRequired]
    public IReadOnlyList<OmniOption<TValue>> Options { get; set; } = Array.Empty<OmniOption<TValue>>();

    [Parameter]
    public int VisibleRows { get; set; } = 5;

    /// <summary>
    /// Compact keeps the control on a single line and opens its list on demand, which is what a
    /// filter sitting in a toolbar needs. List, the default, stays an always-open native list.
    /// </summary>
    [Parameter]
    public OmniMultiSelectPresentation Presentation { get; set; } = OmniMultiSelectPresentation.List;

    /// <summary>Shown by the compact presentation while nothing is selected.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    /// Adds a search field to the compact panel and narrows the list to the options whose text
    /// contains what was typed. Only the compact presentation can carry it: the list presentation is
    /// a native multiple select, which has nowhere to put a field and addresses its options by
    /// position, so filtering it would silently select the wrong ones.
    /// </summary>
    [Parameter]
    public bool Filterable { get; set; }

    /// <summary>
    /// The text being searched for. Bind it when the page has to act on what was typed rather than
    /// only see the narrowed list, such as offering to create the entry nobody matched; setting it
    /// back to null empties the field.
    /// </summary>
    [Parameter]
    public string? FilterText { get; set; }

    [Parameter]
    public EventCallback<string?> FilterTextChanged { get; set; }

    /// <summary>Replaces the default label and placeholder of the search field.</summary>
    [Parameter]
    public string? FilterPlaceholder { get; set; }

    /// <summary>
    /// Renders an option next to its check box, for a colour swatch or a second line. The option's
    /// text is what the filter matches on, whatever this draws.
    /// </summary>
    [Parameter]
    public RenderFragment<OmniOption<TValue>>? OptionTemplate { get; set; }

    /// <summary>Sits at the bottom of the compact panel, below the list and outside its scroll.</summary>
    [Parameter]
    public RenderFragment? FooterTemplate { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private int SelectedCount => CurrentValue?.Count ?? 0;

    private string? Filter => _filter;

    /// <summary>
    /// What the panel lists. Selected options are not exempt from the filter: the summary already
    /// counts them, so keeping them visible would contradict the search that was just typed.
    /// </summary>
    private IReadOnlyList<OmniOption<TValue>> VisibleOptions
    {
        get
        {
            if (!Filterable || string.IsNullOrWhiteSpace(_filter))
            {
                return Options;
            }

            var needle = _filter.Trim();
            return [.. Options.Where(option =>
                option.Text.Contains(needle, StringComparison.CurrentCultureIgnoreCase))];
        }
    }

    /// <summary>
    /// One selection is named, several are counted: a control on a single line cannot grow with the
    /// number of entries a filter holds.
    /// </summary>
    private string SummaryText
    {
        get
        {
            if (SelectedCount == 0)
            {
                return string.IsNullOrWhiteSpace(Placeholder) ? Localize("MultiSelectEmpty") : Placeholder;
            }

            if (SelectedCount == 1)
            {
                var only = CurrentValue![0];
                var match = Options.FirstOrDefault(option => EqualityComparer<TValue>.Default.Equals(option.Value, only));
                if (match is not null)
                {
                    return match.Text;
                }
            }

            return Localize("MultiSelectSelected", SelectedCount);
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Filterable && Presentation != OmniMultiSelectPresentation.Compact)
        {
            throw new InvalidOperationException(
                "Filterable requires OmniMultiSelectPresentation.Compact.");
        }

        // The parameter wins whenever the page changes it, which is how a caller empties the field
        // after acting on the text; between those changes the field owns what it holds, so a render
        // caused by anything else does not undo the keystroke being typed.
        if (!string.Equals(_boundFilterText, FilterText, StringComparison.Ordinal))
        {
            _boundFilterText = FilterText;
            _filter = FilterText;
        }
    }

    private bool IsSelected(TValue value) =>
        CurrentValue?.Contains(value, EqualityComparer<TValue>.Default) == true;

    private async Task SetFilterAsync(string? value)
    {
        if (string.Equals(_filter, value, StringComparison.Ordinal))
        {
            return;
        }

        _filter = value;
        _boundFilterText = value;

        if (FilterTextChanged.HasDelegate)
        {
            await FilterTextChanged.InvokeAsync(value);
        }
    }

    private void Toggle(OmniOption<TValue> option, bool selected)
    {
        if (selected == IsSelected(option.Value))
        {
            return;
        }

        var current = CurrentValue ?? [];
        CurrentValue = selected
            ? [.. current, option.Value]
            : [.. current.Where(value => !EqualityComparer<TValue>.Default.Equals(value, option.Value))];
    }

    private void Clear() => CurrentValue = [];

    private void HandleChange(ChangeEventArgs args)
    {
        var keys = args.Value switch
        {
            string[] values => values,
            IEnumerable<string> values => values.ToArray(),
            string value => [value],
            _ => Array.Empty<string>()
        };

        CurrentValue = keys
            .Select(key => int.TryParse(key, out var index) ? index : -1)
            .Where(index => index >= 0 && index < Options.Count && !Options[index].Disabled)
            .Select(index => Options[index].Value)
            .ToArray();
    }

    protected override bool TryParseValueFromString(string? value, out IReadOnlyList<TValue> result, out string validationErrorMessage)
    {
        result = Array.Empty<TValue>();
        validationErrorMessage = Localize("MultiSelectInvalid");
        return false;
    }
}
