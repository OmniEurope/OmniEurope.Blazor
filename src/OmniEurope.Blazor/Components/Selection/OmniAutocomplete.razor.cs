namespace OmniEurope.Blazor.Components;

public partial class OmniAutocomplete<TValue>
{
    private CancellationTokenSource? _searchCancellation;
    private int _searchGeneration;
    private IReadOnlyList<OmniOption<TValue>> _results = Array.Empty<OmniOption<TValue>>();
    private string _searchText = string.Empty;
    private string _announcement = string.Empty;
    private Exception? _error;

    [Parameter, EditorRequired]
    public Func<string, CancellationToken, Task<IReadOnlyList<OmniOption<TValue>>>>? Search { get; set; }

    [Parameter]
    public int DebounceMilliseconds { get; set; } = 250;

    [Parameter]
    public int MinimumLength { get; set; } = 1;

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    [Parameter]
    public Func<TValue, string>? FormatValue { get; set; }

    [Parameter]
    public string SearchErrorMessage { get; set; } = string.Empty;

    [Parameter]
    public RenderFragment<Exception>? ErrorContent { get; set; }

    [Parameter]
    public EventCallback<Exception> SearchFailed { get; set; }

    private string ResultsId => $"{Id ?? FieldIdentifier.FieldName}-results";
    private string ErrorId => $"{Id ?? FieldIdentifier.FieldName}-error";
    private string EffectiveSearchErrorMessage => string.IsNullOrWhiteSpace(SearchErrorMessage)
        ? Localize("AutocompleteSearchFailed")
        : SearchErrorMessage;
    private string? CombinedAriaDescribedBy => _error is null
        ? AriaDescribedBy
        : string.Join(' ', new[] { AriaDescribedBy, ErrorId }.Where(value => !string.IsNullOrWhiteSpace(value)));
    private bool IsSelected(TValue value) => EqualityComparer<TValue>.Default.Equals(CurrentValue, value);

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (string.IsNullOrEmpty(_searchText) && CurrentValue is not null)
        {
            _searchText = FormatValue?.Invoke(CurrentValue) ?? CurrentValue.ToString() ?? string.Empty;
        }
    }

    private async Task HandleInputAsync(ChangeEventArgs args)
    {
        _searchText = args.Value?.ToString() ?? string.Empty;
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = new CancellationTokenSource();
        var token = _searchCancellation.Token;
        var generation = ++_searchGeneration;
        _error = null;

        if (_searchText.Length < MinimumLength || Search is null)
        {
            _results = Array.Empty<OmniOption<TValue>>();
            _announcement = string.Empty;
            return;
        }

        try
        {
            await Task.Delay(Math.Max(0, DebounceMilliseconds), token);
            var results = await Search(_searchText, token);
            if (generation != _searchGeneration)
            {
                return;
            }

            _results = results;
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
                _announcement = EffectiveSearchErrorMessage;
                await SearchFailed.InvokeAsync(exception);
            }
        }
    }

    private void Select(OmniOption<TValue> option)
    {
        if (option.Disabled)
        {
            return;
        }

        CurrentValue = option.Value;
        _searchText = option.Text;
        _results = Array.Empty<OmniOption<TValue>>();
        _announcement = Localize("AutocompleteSelected", option.Text);
    }

    protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
    {
        result = default!;
        validationErrorMessage = Localize("AutocompleteInvalid");
        return false;
    }

    public ValueTask DisposeAsync()
    {
        _searchGeneration++;
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        return ValueTask.CompletedTask;
    }
}
