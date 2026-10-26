using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A multiple selection on a single line: a summary that names or counts the selected options and
/// opens, on demand, a panel of check boxes (with an optional search field). For an always-open list,
/// use <see cref="OmniListBox{TValue, TSelection}"/> with <c>Multiple</c>, or <see cref="OmniCheckBoxList{TValue}"/>.
/// </summary>
/// <typeparam name="TValue">The value of each option; the component binds a list of them.</typeparam>
public partial class OmniMultiSelect<TValue>
{
    private string? _filter;
    private string? _boundFilterText;
    private ElementReference _details;
    private OmniDisclosureDismissal? _dismissal;
    private OmniPackageTooltips? _tooltips;

    /// <summary>The options, in order; the value lists the selected ones in the order they were checked.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<OmniOption<TValue>> Options { get; set; } = Array.Empty<OmniOption<TValue>>();

    /// <summary>
    /// The accessible name of the control (its field name, which the summary line alone, a count or the
    /// chosen text, does not give). Null or blank uses the label of an enclosing <see cref="OmniFormField"/>
    /// whose <c>For</c> is <see cref="OmniInputBase{TValue}.Id"/>; without either, the summary names it, as before.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Shown on the summary line while nothing is selected.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    /// Adds a search field to the panel and narrows the list to the options whose text contains what
    /// was typed.
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

    /// <summary>Raised with the new search text each time the user changes it.</summary>
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

    /// <summary>Sits at the bottom of the panel, below the list and outside its scroll.</summary>
    [Parameter]
    public RenderFragment? FooterContent { get; set; }

    /// <summary>Whether the field is disabled. Off by default.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Whether the selection can be read and the field focused but not changed: the panel opens to show the
    /// boxes, none of which a click or a key can change, and there is no Clear button. Not dimmed.
    /// Off by default.
    /// </summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Identifiers of the elements that describe the field, written as <c>aria-describedby</c>; none when null.</summary>
    [Parameter]
    public string? AriaDescribedBy { get; set; }

    /// <summary>
    /// Closes the open panel when a press lands outside it. On by default; a page that drives the
    /// field from controls of its own can turn it off, or mark those controls with
    /// <c>data-omni-keep-open</c> so a press on them never counts as outside. Escape closes the panel
    /// either way.
    /// </summary>
    [Parameter]
    public bool CloseOnOutsideClick { get; set; } = true;

    private readonly string _generatedId = $"omni-multi-select-{Guid.NewGuid():N}";

    private string SummaryTextId => $"{Id ?? _generatedId}-text";

    private bool HasLabel => !string.IsNullOrWhiteSpace(Label);

    // The summary line is the value (a count, the chosen text); the field name comes before it.
    private string? SummaryAriaLabel => HasLabel ? $"{Label}: {SummaryText}" : null;

    private string? SummaryLabelledBy => !HasLabel && FormFieldLabelId is { } labelId ? $"{labelId} {SummaryTextId}" : null;

    private string? PanelAriaLabel => HasLabel ? Label : FormFieldLabelId is null ? SummaryText : null;

    private string? PanelLabelledBy => !HasLabel ? FormFieldLabelId : null;

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
            return [.. Options.Where(option => OmniTextMatch.Contains(option.Text, needle))];
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

    /// <summary>Takes a new <see cref="FilterText"/> from the page; text being typed stays while the page passes the same value.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

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
        if (ReadOnly || selected == IsSelected(option.Value))
        {
            return;
        }

        var current = CurrentValue ?? [];
        CurrentValue = selected
            ? [.. current, option.Value]
            : [.. current.Where(value => !EqualityComparer<TValue>.Default.Equals(value, option.Value))];
    }

    private void Clear() => CurrentValue = [];

    /// <summary>Never parses: the field sets its value from the boxes checked, never from text.</summary>
    /// <param name="value">The text, ignored.</param>
    /// <param name="result">Always an empty list.</param>
    /// <param name="validationErrorMessage">The localized "invalid selection" message.</param>
    /// <returns>Always false.</returns>
    protected override bool TryParseValueFromString(string? value, out IReadOnlyList<TValue> result, out string validationErrorMessage)
    {
        result = Array.Empty<TValue>();
        validationErrorMessage = Localize("MultiSelectInvalid");
        return false;
    }

    /// <summary>
    /// Applies <see cref="CloseOnOutsideClick"/> to the panel's dismissal script and, on the first render, turns
    /// the package tooltips on so a summary cut by its ellipsis is read whole on hover and on keyboard focus.
    /// </summary>
    /// <param name="firstRender">True on the first render of the component.</param>
    /// <returns>A task that completes once the dismissal is applied.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _tooltips = new OmniPackageTooltips(JavaScript);
            await _tooltips.InstallAsync();
        }

        _dismissal ??= new OmniDisclosureDismissal(JavaScript);
        await _dismissal.ApplyAsync(_details, CloseOnOutsideClick, closeOnItem: false);
    }

    /// <summary>Releases the form subscription, the dismissal script and the package tooltips.</summary>
    /// <returns>A task that completes once the script is released.</returns>
    public async ValueTask DisposeAsync()
    {
        // Blazor calls only DisposeAsync on a component that has both: the form subscription of
        // InputBase is released through its own Dispose.
        ((IDisposable)this).Dispose();
        if (_dismissal is not null)
        {
            await _dismissal.DisposeAsync();
        }

        if (_tooltips is not null)
        {
            await _tooltips.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
