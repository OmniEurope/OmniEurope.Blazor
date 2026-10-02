using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A text field that suggests values as the user types, following the editable combobox pattern: the
/// arrows move through the suggestions while the focus stays in the field, Enter picks the highlighted
/// one, Escape closes the list, and Home and End reach the first and last suggestion while one is highlighted.
/// </summary>
/// <typeparam name="TValue">The value each suggestion carries.</typeparam>
public partial class OmniAutocomplete<TValue>
{
    private CancellationTokenSource? _searchCancellation;
    private int _searchGeneration;
    private IReadOnlyList<OmniOption<TValue>> _results = Array.Empty<OmniOption<TValue>>();
    private string _searchText = string.Empty;
    private string _resultsQuery = string.Empty;
    private string _announcement = string.Empty;
    private Exception? _error;
    private TValue? _shownValue;
    private bool _hasShownValue;
    private int _activeIndex = -1;
    private bool _closed;

    /// <summary>
    /// Finds the suggestions for what was typed. It receives the text and a token cancelled when a newer
    /// keystroke supersedes the search.
    /// </summary>
    [Parameter, EditorRequired]
    public Func<string, CancellationToken, Task<IReadOnlyList<OmniOption<TValue>>>>? Search { get; set; }

    /// <summary>
    /// The pause after the last keystroke before <see cref="Search"/> runs; 250 ms by default.
    /// <see cref="TimeSpan.Zero"/> searches at once; a negative value counts as zero.
    /// </summary>
    [Parameter]
    public TimeSpan Debounce { get; set; } = TimeSpan.FromMilliseconds(250);

    /// <summary>The number of characters typed before a search runs; 1 by default, 0 also searches an empty field.</summary>
    [Parameter]
    public int MinimumLength { get; set; } = 1;

    /// <summary>Hint shown in the empty field. Null shows none.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    /// The accessible name of the field, written as its <c>aria-label</c>. Null, the default, writes none,
    /// so the <c>label</c> of an enclosing <see cref="OmniFormField"/> (or any <c>label for</c> the
    /// <see cref="OmniInputBase{TValue}.Id"/>) names it.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Whether the field and its suggestions are disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Ids of elements that describe the field, written in its <c>aria-describedby</c> before the error line.</summary>
    [Parameter]
    public string? AriaDescribedBy { get; set; }

    /// <summary>Turns a value set by the parent into the text shown in the field; its <c>ToString()</c> when null.</summary>
    [Parameter]
    public Func<TValue, string>? FormatValue { get; set; }

    /// <summary>The message shown when <see cref="Search"/> throws; null uses the localized default.</summary>
    [Parameter]
    public string? SearchErrorMessage { get; set; }

    /// <summary>Replaces the error message with content built from the exception <see cref="Search"/> threw.</summary>
    [Parameter]
    public RenderFragment<Exception>? ErrorContent { get; set; }

    /// <summary>Raised with the exception when <see cref="Search"/> throws; a cancelled search is not an error.</summary>
    [Parameter]
    public EventCallback<Exception> OnSearchError { get; set; }

    /// <summary>
    /// Marks, in each suggestion, the letters that match what was typed, ignoring case and accents
    /// so that "liege" marks the accented city name. On by default: a list the eye has to read in
    /// full to see why each entry is there is slower to choose from. The text read aloud is unchanged.
    /// </summary>
    [Parameter]
    public bool HighlightMatches { get; set; } = true;

    /// <summary>
    /// Drawn before each suggestion's text, for an icon or a flag that says what the entry is. It is
    /// decoration, hidden from assistive technology: the text, with its match highlighting, still names
    /// the entry.
    /// </summary>
    [Parameter]
    public RenderFragment<OmniOption<TValue>>? OptionIconTemplate { get; set; }

    private string BaseId => Id ?? FieldIdentifier.FieldName;
    private string ResultsId => $"{BaseId}-results";
    private string ErrorId => $"{BaseId}-error";
    private string OptionId(int index) => $"{ResultsId}-{index.ToString(CultureInfo.InvariantCulture)}";
    private bool IsOpen => !_closed && _results.Count > 0;
    private string? ActiveOptionId => IsOpen && _activeIndex >= 0 && _activeIndex < _results.Count ? OptionId(_activeIndex) : null;

    // Class goes on the outermost element; the validation classes of the form stay on the input they describe.
    private string RootClass => CssClassBuilder.Combine(["omni-autocomplete", Class]);
    private string InputClass => CssClassBuilder.Combine(["omni-input", "omni-autocomplete__input", SizeClass, CssClass]);

    private string EffectiveSearchErrorMessage => string.IsNullOrWhiteSpace(SearchErrorMessage)
        ? Localize("AutocompleteSearchFailed")
        : SearchErrorMessage;
    private string? CombinedAriaDescribedBy => _error is null
        ? AriaDescribedBy
        : string.Join(' ', new[] { AriaDescribedBy, ErrorId }.Where(value => !string.IsNullOrWhiteSpace(value)));
    private bool IsSelected(TValue value) => EqualityComparer<TValue>.Default.Equals(CurrentValue, value);

    private string OptionClass(int index, OmniOption<TValue> option) => CssClassBuilder.Combine([
        "omni-autocomplete__option",
        index == _activeIndex ? "omni-autocomplete__option--active" : null,
        IsSelected(option.Value) ? "omni-autocomplete__option--selected" : null,
        option.Disabled || Disabled ? "omni-autocomplete__option--disabled" : null]);

    /// <summary>
    /// When the parent replaced the value, writes it in the field (through <see cref="FormatValue"/>, else
    /// its <c>ToString</c>; empty for null) and drops the suggestions and any search still running. Text
    /// the user typed is left alone otherwise.
    /// </summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Only a value the parent replaced rewrites the text: what the user types leaves the value as
        // it is, and a choice made here is already shown with its option's text.
        if (_hasShownValue && EqualityComparer<TValue>.Default.Equals(CurrentValue, _shownValue))
        {
            return;
        }

        var replaced = _hasShownValue;
        _shownValue = CurrentValue;
        _hasShownValue = true;
        if (CurrentValue is not null)
        {
            _searchText = FormatValue?.Invoke(CurrentValue) ?? CurrentValue.ToString() ?? string.Empty;
        }
        else if (replaced)
        {
            _searchText = string.Empty;
        }

        if (replaced)
        {
            // The suggestions and any search still on its way were for the former text.
            _searchGeneration++;
            _searchCancellation?.Cancel();
            _results = Array.Empty<OmniOption<TValue>>();
            _activeIndex = -1;
            _announcement = string.Empty;
            _error = null;
        }
    }

    private async Task HandleInputAsync(ChangeEventArgs args)
    {
        _searchText = args.Value?.ToString() ?? string.Empty;
        _closed = false;
        _activeIndex = -1;
        _error = null;
        var (generation, token) = RestartSearch();

        if (_searchText.Length < MinimumLength || Search is null)
        {
            _results = Array.Empty<OmniOption<TValue>>();
            _announcement = string.Empty;
            return;
        }

        await RunSearchAsync(Debounce, generation, token);
    }

    private (int Generation, CancellationToken Token) RestartSearch()
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = new CancellationTokenSource();
        return (++_searchGeneration, _searchCancellation.Token);
    }

    private async Task RunSearchAsync(TimeSpan delay, int generation, CancellationToken token)
    {
        try
        {
            await Task.Delay(delay < TimeSpan.Zero ? TimeSpan.Zero : delay, token);
            var results = await Search!(_searchText, token);
            if (generation != _searchGeneration)
            {
                return;
            }

            _results = results;
            _resultsQuery = _searchText;
            _activeIndex = -1;
            _announcement = _results.Count == 1
                ? Localize("AutocompleteOneResult")
                : Localize("AutocompleteManyResults", _results.Count);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (generation == _searchGeneration)
            {
                _error = exception;
                _results = Array.Empty<OmniOption<TValue>>();
                _activeIndex = -1;
                _announcement = EffectiveSearchErrorMessage;
                await OnSearchError.InvokeAsync(exception);
            }
        }
    }

    /// <summary>
    /// The keyboard of the editable combobox. The arrows open the list (searching the text in the field
    /// when nothing is listed yet) and move the visual focus, wrapping at both ends; Home and End jump to
    /// the ends only while a suggestion is highlighted, and otherwise keep moving the caret in the text;
    /// Enter picks the highlighted suggestion; Escape closes the list.
    /// </summary>
    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (Disabled)
        {
            return;
        }

        switch (args.Key)
        {
            case "ArrowDown":
                await OpenOrMoveAsync(forward: true);
                break;
            case "ArrowUp":
                await OpenOrMoveAsync(forward: false);
                break;
            case "Home" when IsOpen && _activeIndex >= 0:
                _activeIndex = 0;
                break;
            case "End" when IsOpen && _activeIndex >= 0:
                _activeIndex = _results.Count - 1;
                break;
            case "Enter" when IsOpen && _activeIndex >= 0 && _activeIndex < _results.Count:
                Select(_results[_activeIndex]);
                break;
            case "Escape" when IsOpen:
                Close();
                break;
        }
    }

    private async Task OpenOrMoveAsync(bool forward)
    {
        if (IsOpen)
        {
            _activeIndex = forward
                ? (_activeIndex + 1 >= _results.Count ? 0 : _activeIndex + 1)
                : (_activeIndex <= 0 ? _results.Count - 1 : _activeIndex - 1);
            return;
        }

        if (_results.Count == 0 && Search is not null && _searchText.Length >= MinimumLength)
        {
            // Nothing listed yet (a value set by the parent, a field just focused): search the text in
            // the field now, without the typing pause.
            _error = null;
            var (generation, token) = RestartSearch();
            await RunSearchAsync(TimeSpan.Zero, generation, token);
        }

        _closed = false;
        _activeIndex = _results.Count == 0 ? -1 : forward ? 0 : _results.Count - 1;
    }

    private void Close()
    {
        _closed = true;
        _activeIndex = -1;
    }

    private void Select(OmniOption<TValue> option)
    {
        if (option.Disabled || Disabled)
        {
            return;
        }

        _shownValue = option.Value;
        _hasShownValue = true;
        CurrentValue = option.Value;
        _searchText = option.Text;
        _results = Array.Empty<OmniOption<TValue>>();
        _activeIndex = -1;
        _announcement = Localize("AutocompleteSelected", option.Text);
    }

    /// <summary>Never parses: the value is set by choosing a suggestion, never from the typed text.</summary>
    /// <param name="value">The text, ignored.</param>
    /// <param name="result">Always the default value.</param>
    /// <param name="validationErrorMessage">The localized "invalid value" message.</param>
    /// <returns>Always false.</returns>
    protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
    {
        result = default!;
        validationErrorMessage = Localize("AutocompleteInvalid");
        return false;
    }

    /// <summary>Cancels a pending search and releases the form subscription.</summary>
    public ValueTask DisposeAsync()
    {
        // Blazor calls only DisposeAsync on a component that has both: the form subscription of
        // InputBase is released through its own Dispose.
        ((IDisposable)this).Dispose();
        _searchGeneration++;
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        return ValueTask.CompletedTask;
    }
}
