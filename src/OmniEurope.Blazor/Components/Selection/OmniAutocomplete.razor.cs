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
    // The text that stands for the current value: a free text equal to it is not taken again.
    private string _committedText = string.Empty;
    private ElementReference _input;

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

    /// <summary>
    /// Whether the value can be read and the field focused but not changed: the text is <c>readonly</c>, the
    /// keyboard opens no list and picks no suggestion, and the field is not dimmed. A <c>readonly</c> attribute
    /// passed through has the same effect. Off by default.
    /// </summary>
    [Parameter]
    public bool ReadOnly { get; set; }

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

    /// <summary>
    /// Draws at the end of the field the chevron of a drop-down list, for a field that offers a closed
    /// list (as <see cref="OmniDropDown{TValue}"/> does with <c>Filterable</c>): a press on it opens the
    /// whole list, unfiltered (<see cref="Search"/> called with an empty text), and a second press closes
    /// it. It is not a tab stop; the keyboard keeps the arrows. It follows <see cref="Disabled"/> and a
    /// <c>readonly</c> attribute. False by default: a field that searches an open set has nothing to list.
    /// </summary>
    [Parameter]
    public bool ShowToggle { get; set; }

    /// <summary>
    /// Takes the text typed as the value, for a field whose suggestions are a help rather than the only
    /// choices (a known list plus any name the list lacks). The text is taken when the field is left, and
    /// on Enter while no suggestion is highlighted; a highlighted suggestion is still picked by Enter, and a
    /// suggestion whose text is the one typed, ignoring case, gives its own value. A field emptied (or left
    /// with spaces only) gives the default value. False by default: only a suggestion sets the value.
    /// </summary>
    [Parameter]
    public bool AllowFreeText { get; set; }

    /// <summary>
    /// Turns the text typed into the value when <see cref="AllowFreeText"/> takes it; required unless
    /// <typeparamref name="TValue"/> is <see cref="string"/>, where the text itself is the value. A
    /// <see cref="FormatException"/>, <see cref="OverflowException"/> or <see cref="ArgumentException"/> it
    /// throws leaves the value unchanged and marks the field invalid in its form, with the localized
    /// "invalid value" message.
    /// </summary>
    [Parameter]
    public Func<string, TValue>? ParseFreeText { get; set; }

    private readonly string _generatedId = $"omni-autocomplete-{Guid.NewGuid():N}";
    private string BaseId => Id ?? _generatedId;
    private string ResultsId => $"{BaseId}-results";
    private string ErrorId => $"{BaseId}-error";
    private string OptionId(int index) => $"{ResultsId}-{index.ToString(CultureInfo.InvariantCulture)}";
    private bool IsOpen => !_closed && _results.Count > 0;
    // Every change of the suggestions resets the highlight, so a highlighted index is always one of them.
    private string? ActiveOptionId => IsOpen && _activeIndex >= 0 ? OptionId(_activeIndex) : null;

    // Class goes on the outermost element; the validation classes of the form stay on the input they describe.
    private string RootClass => CssClassBuilder.Combine(["omni-autocomplete", ShowToggle ? "omni-autocomplete--toggle" : null, Class]);
    private string ToggleClass => CssClassBuilder.Combine([
        "omni-autocomplete__toggle",
        IsOpen ? "omni-autocomplete__toggle--open" : null,
        Disabled || IsReadOnly ? "omni-autocomplete__toggle--disabled" : null]);

    // The ReadOnly parameter; a readonly attribute written on the tag reaches it too (parameter names are
    // matched without case), so a false value leaves the field editable.
    private bool IsReadOnly => ReadOnly;
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
        if (AllowFreeText && ParseFreeText is null && typeof(TValue) != typeof(string))
        {
            throw new InvalidOperationException(
                $"{nameof(OmniAutocomplete<TValue>)} with {nameof(AllowFreeText)} needs {nameof(ParseFreeText)} to turn the text into a {typeof(TValue).Name}.");
        }

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

        _committedText = _searchText;
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
        if (IsReadOnly)
        {
            return;
        }

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

    private async Task RunSearchAsync(TimeSpan delay, int generation, CancellationToken token, string? query = null)
    {
        var text = query ?? _searchText;
        try
        {
            await Task.Delay(delay < TimeSpan.Zero ? TimeSpan.Zero : delay, token);
            // A search that answers with nothing at all lists nothing, as one that answers with an empty list.
            var results = await Search!(text, token) ?? Array.Empty<OmniOption<TValue>>();
            if (generation != _searchGeneration)
            {
                return;
            }

            _results = results;
            _resultsQuery = text;
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
        if (Disabled || IsReadOnly)
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
            case "Enter" when IsOpen && _activeIndex >= 0:
                Select(_results[_activeIndex]);
                break;
            case "Enter" when AllowFreeText:
                Close();
                CommitFreeText();
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

    /// <summary>
    /// The chevron: opens the whole list, unfiltered, or closes it when it is open, then leaves the
    /// focus in the field so the keyboard goes on from there.
    /// </summary>
    private async Task ToggleListAsync()
    {
        if (Disabled || IsReadOnly || Search is null)
        {
            return;
        }

        if (IsOpen)
        {
            Close();
            return;
        }

        _error = null;
        var (generation, token) = RestartSearch();
        await RunSearchAsync(TimeSpan.Zero, generation, token, string.Empty);
        _closed = false;
        _activeIndex = -1;
        await _input.FocusAsync(preventScroll: true);
    }

    private void Close()
    {
        _closed = true;
        _activeIndex = -1;
    }

    private void HandleBlur()
    {
        Close();
        if (AllowFreeText)
        {
            CommitFreeText();
        }
    }

    /// <summary>
    /// Takes the text in the field as the value (<see cref="AllowFreeText"/>): the listed suggestion of the
    /// same text when there is one, else the text through <see cref="ParseFreeText"/>, the form's parsing
    /// path, so a text it refuses is reported as an invalid value. The text that already stands for the
    /// value is not taken again.
    /// </summary>
    private void CommitFreeText()
    {
        if (Disabled || IsReadOnly || string.Equals(_searchText, _committedText, StringComparison.Ordinal))
        {
            return;
        }

        var text = _searchText.Trim();
        var match = _results.FirstOrDefault(option => !option.Disabled && string.Equals(option.Text, text, StringComparison.CurrentCultureIgnoreCase));
        if (match is not null)
        {
            Select(match);
            return;
        }

        _committedText = _searchText;
        CurrentValueAsString = text;
    }

    private void Select(OmniOption<TValue> option)
    {
        if (option.Disabled || Disabled || IsReadOnly)
        {
            return;
        }

        _shownValue = option.Value;
        _hasShownValue = true;
        CurrentValue = option.Value;
        _searchText = option.Text;
        _committedText = option.Text;
        _results = Array.Empty<OmniOption<TValue>>();
        _activeIndex = -1;
        _announcement = Localize("AutocompleteSelected", option.Text);
    }

    /// <summary>
    /// Parses the text typed only with <see cref="AllowFreeText"/>: an empty text gives the default value, a
    /// <see cref="string"/> value is the text itself, any other goes through <see cref="ParseFreeText"/>.
    /// Without it, the value is set by choosing a suggestion, never from the typed text.
    /// </summary>
    /// <param name="value">The text typed.</param>
    /// <param name="result">The value the text stands for, the default value when it is refused.</param>
    /// <param name="validationErrorMessage">The localized "invalid value" message when the text is refused.</param>
    /// <returns>Whether the text gave a value.</returns>
    protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
    {
        result = default!;
        validationErrorMessage = Localize("AutocompleteInvalid");
        if (!AllowFreeText)
        {
            return false;
        }

        try
        {
            result = string.IsNullOrWhiteSpace(value) ? default!
                : ParseFreeText is not null ? ParseFreeText(value)
                : (TValue)(object)value;
        }
        catch (Exception exception) when (exception is FormatException or OverflowException or ArgumentException)
        {
            return false;
        }

        // The text stays as typed: the parent's echo of the new value must not rewrite it.
        _shownValue = result;
        _hasShownValue = true;
        validationErrorMessage = string.Empty;
        return true;
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
