namespace OmniEurope.Blazor.Components;

public partial class OmniSelectBar<TValue>
{
    private ElementReference _strip;
    private IJSObjectReference? _module;

    [Parameter, EditorRequired]
    public IReadOnlyList<OmniOption<TValue>> Options { get; set; } = Array.Empty<OmniOption<TValue>>();

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    private bool IsSelected(TValue value) => EqualityComparer<TValue>.Default.Equals(CurrentValue, value);

    private void Select(TValue? value)
    {
        if (!Disabled)
        {
            CurrentValue = value!;
        }
    }

    protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
    {
        result = default!;
        validationErrorMessage = Localize("SelectBarInvalid");
        return false;
    }

    /// <summary>
    /// The chevrons follow the scroll position, which only the browser knows: the shared overflow
    /// script owns them, as it does a tab strip's.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JavaScript.InvokeAsync<IJSObjectReference>("import", "./_content/OmniEurope.Blazor/omni-focus.js");
            await _module.InvokeVoidAsync("configureSelectBarOverflow", _strip);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("disposeTabsOverflow", _strip);
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        GC.SuppressFinalize(this);
    }
}
