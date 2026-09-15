namespace OmniEurope.Blazor.Components;

/// <summary>
/// Picks one or several entities (roles, users, servers) from a provider the host searches: the
/// chosen ones show as chips with a remove button, the found ones as a list of toggles, the searched
/// letters marked. <see cref="Layout"/> puts the chips above the list, or the available and chosen
/// items in two columns.
/// </summary>
/// <typeparam name="TItem">The entity type.</typeparam>
/// <typeparam name="TKey">What identifies an entity, compared to tell whether it is chosen.</typeparam>
/// <remarks>
/// The component calls <see cref="Search"/> with the typed text (and once with an empty text when it
/// appears), after <see cref="DebounceMilliseconds"/>, cancelling a search a newer one replaces; it
/// opens no connection of its own. A failed search shows a message and a retry button.
/// </remarks>
public partial class OmniEntityPicker<TItem, TKey> : IAsyncDisposable
{
    private readonly string _generatedId = $"omni-entity-picker-{Guid.NewGuid():N}";
    private CancellationTokenSource? _searchCancellation;
    private int _searchGeneration;
    private IReadOnlyList<TItem> _results = Array.Empty<TItem>();
    private string _query = string.Empty;
    private string _resultsQuery = string.Empty;
    private string _announcement = string.Empty;
    private Exception? _error;
    private bool _loading;
    private bool _searched;
    private bool _showAllChips;
    private ElementReference _selectionRegion;
    private Func<string, CancellationToken, Task<IReadOnlyList<TItem>>>? _observedSearch;

    /// <summary>Finds the entities matching a text; an empty text asks for the first ones to show.</summary>
    [Parameter, EditorRequired]
    public Func<string, CancellationToken, Task<IReadOnlyList<TItem>>> Search { get; set; } = default!;

    /// <summary>What identifies an entity.</summary>
    [Parameter, EditorRequired]
    public Func<TItem, TKey> KeySelector { get; set; } = default!;

    /// <summary>The name of an entity, shown in the list and on its chip.</summary>
    [Parameter, EditorRequired]
    public Func<TItem, string> TextSelector { get; set; } = default!;

    /// <summary>A second line under the name in the list.</summary>
    [Parameter]
    public Func<TItem, string?>? DescriptionSelector { get; set; }

    /// <summary>Replaces the name and description of an entity in the list.</summary>
    [Parameter]
    public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>The chosen entities.</summary>
    [Parameter]
    public IReadOnlyList<TItem> Selected { get; set; } = Array.Empty<TItem>();

    /// <summary>Raised with a new list whenever an entity is added or removed.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<TItem>> SelectedChanged { get; set; }

    /// <summary>Whether several entities may be chosen; one alone replaces the other when false.</summary>
    [Parameter]
    public bool Multiple { get; set; } = true;

    /// <summary>Chips above the list, or two columns.</summary>
    [Parameter]
    public OmniEntityPickerLayout Layout { get; set; } = OmniEntityPickerLayout.Stacked;

    /// <summary>The name of the group; the localized "selection" when empty.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Placeholder and accessible name of the search box.</summary>
    [Parameter]
    public string? SearchPlaceholder { get; set; }

    /// <summary>Title of the available column in <see cref="OmniEntityPickerLayout.TwoColumns"/>.</summary>
    [Parameter]
    public string? AvailableTitle { get; set; }

    /// <summary>Title of the chosen column in <see cref="OmniEntityPickerLayout.TwoColumns"/>.</summary>
    [Parameter]
    public string? SelectedTitle { get; set; }

    /// <summary>What an empty search result says.</summary>
    [Parameter]
    public string? NoResultsText { get; set; }

    /// <summary>What the selection says while nothing is chosen.</summary>
    [Parameter]
    public string? NoneSelectedText { get; set; }

    /// <summary>What a failed search says.</summary>
    [Parameter]
    public string? SearchErrorText { get; set; }

    /// <summary>How many chips show before a button reveals the rest; all of them when null.</summary>
    [Parameter]
    public int? MaxVisibleChips { get; set; }

    /// <summary>How long typing must pause before the search runs.</summary>
    [Parameter]
    public int DebounceMilliseconds { get; set; } = 250;

    /// <summary>Disables the search, the options and the remove buttons.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Raised when <see cref="Search"/> fails.</summary>
    [Parameter]
    public EventCallback<Exception> SearchFailed { get; set; }

    private string RootId => Id ?? _generatedId;
    private string LabelId => $"{RootId}-label";
    private string ResultsId => $"{RootId}-results";
    private string AvailableTitleId => $"{RootId}-available";
    private string ChosenTitleId => $"{RootId}-chosen";
    private string LayoutName => Layout == OmniEntityPickerLayout.TwoColumns ? "two-columns" : "stacked";

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("EntityPickerLabel") : Label;
    private string EffectiveSearchPlaceholder => string.IsNullOrWhiteSpace(SearchPlaceholder) ? Localize("EntityPickerSearch") : SearchPlaceholder;
    private string EffectiveAvailableTitle => string.IsNullOrWhiteSpace(AvailableTitle) ? Localize("EntityPickerAvailable") : AvailableTitle;
    private string EffectiveSelectedTitle => string.IsNullOrWhiteSpace(SelectedTitle) ? Localize("EntityPickerSelected") : SelectedTitle;
    private string EffectiveNoResultsText => string.IsNullOrWhiteSpace(NoResultsText) ? Localize("EntityPickerNoResults") : NoResultsText;
    private string EffectiveNoneSelectedText => string.IsNullOrWhiteSpace(NoneSelectedText) ? Localize("EntityPickerNoneSelected") : NoneSelectedText;
    private string EffectiveErrorText => string.IsNullOrWhiteSpace(SearchErrorText) ? Localize("EntityPickerSearchFailed") : SearchErrorText;
    private string SelectedCountText => Localize(Selected.Count == 1 ? "EntityPickerOneSelected" : "EntityPickerManySelected", Selected.Count);

    /// <summary>What the list offers: every result in the stacked layout, the ones not chosen yet in two columns.</summary>
    private IReadOnlyList<TItem> AvailableItems => Layout == OmniEntityPickerLayout.TwoColumns
        ? [.. _results.Where(item => !IsSelected(item))]
        : _results;

    private IEnumerable<TItem> VisibleChips => MaxVisibleChips is { } maximum && !_showAllChips && Layout == OmniEntityPickerLayout.Stacked
        ? Selected.Take(Math.Max(0, maximum))
        : Selected;

    private int HiddenChipCount => MaxVisibleChips is { } maximum && !_showAllChips && Layout == OmniEntityPickerLayout.Stacked
        ? Math.Max(0, Selected.Count - Math.Max(0, maximum))
        : 0;

    protected override async Task OnParametersSetAsync()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(Search);
        ArgumentNullException.ThrowIfNull(KeySelector);
        ArgumentNullException.ThrowIfNull(TextSelector);
        if (Selected is null)
        {
            Selected = Array.Empty<TItem>();
        }

        // The first search, and a new one whenever the host swaps the provider. Delegates compare by
        // target and method: a method group rebuilt at each render of the host is the same provider.
        if (!_searched || !Equals(_observedSearch, Search))
        {
            _searched = true;
            _observedSearch = Search;
            await RunSearchAsync(_query, debounce: false);
        }
    }

    private bool IsSelected(TItem item)
    {
        var key = KeySelector(item);
        return Selected.Any(chosen => EqualityComparer<TKey>.Default.Equals(KeySelector(chosen), key));
    }

    private static string OptionClass(bool chosen) => chosen
        ? "omni-entity-picker__option omni-entity-picker__option--selected"
        : "omni-entity-picker__option";

    private Task SearchChangedAsync(string? text)
    {
        _query = text ?? string.Empty;
        return RunSearchAsync(_query, debounce: true);
    }

    private Task RetryAsync() => RunSearchAsync(_query, debounce: false);

    private async Task RunSearchAsync(string text, bool debounce)
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = new CancellationTokenSource();
        var token = _searchCancellation.Token;
        var generation = ++_searchGeneration;
        _error = null;
        _loading = true;
        try
        {
            if (debounce && DebounceMilliseconds > 0)
            {
                await Task.Delay(DebounceMilliseconds, token);
            }

            var results = await Search(text.Trim(), token);
            if (generation != _searchGeneration)
            {
                return;
            }

            _results = results ?? Array.Empty<TItem>();
            _resultsQuery = text.Trim();
            _announcement = _results.Count == 1 ? Localize("AutocompleteOneResult") : Localize("AutocompleteManyResults", _results.Count);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (generation == _searchGeneration)
            {
                _error = exception;
                _results = Array.Empty<TItem>();
                _announcement = EffectiveErrorText;
                await SearchFailed.InvokeAsync(exception);
            }
        }
        finally
        {
            if (generation == _searchGeneration)
            {
                _loading = false;
            }
        }
    }

    private async Task ToggleAsync(TItem item)
    {
        if (Disabled)
        {
            return;
        }

        if (IsSelected(item))
        {
            await RemoveCoreAsync(item);
            return;
        }

        List<TItem> updated = Multiple ? [.. Selected, item] : [item];
        Selected = updated;
        _announcement = Localize("EntityPickerAdded", TextSelector(item));
        await SelectedChanged.InvokeAsync(updated);
    }

    private async Task RemoveAsync(TItem item)
    {
        await RemoveCoreAsync(item);

        // The removed chip took the focus with it: the selection region keeps it in place.
        try
        {
            await _selectionRegion.FocusAsync();
        }
        catch (JSException)
        {
        }
        catch (InvalidOperationException)
        {
            // Not rendered (prerendering): nothing holds the focus to move.
        }
    }

    private async Task RemoveCoreAsync(TItem item)
    {
        if (Disabled)
        {
            return;
        }

        var key = KeySelector(item);
        List<TItem> updated = [.. Selected.Where(chosen => !EqualityComparer<TKey>.Default.Equals(KeySelector(chosen), key))];
        Selected = updated;
        _announcement = Localize("EntityPickerRemoved", TextSelector(item));
        await SelectedChanged.InvokeAsync(updated);
    }

    private void ShowAllChips() => _showAllChips = true;

    public ValueTask DisposeAsync()
    {
        _searchGeneration++;
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
