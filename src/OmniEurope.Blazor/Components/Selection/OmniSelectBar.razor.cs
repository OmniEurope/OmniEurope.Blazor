namespace OmniEurope.Blazor.Components;

/// <summary>
/// A segmented choice: the options side by side as a radio group of buttons, the selected one
/// highlighted. Too wide for its place, the bar scrolls sideways under a chevron on each side.
/// </summary>
/// <typeparam name="TValue">The type of the value.</typeparam>
public partial class OmniSelectBar<TValue>
{
    private ElementReference _strip;
    private IJSObjectReference? _module;

    /// <summary>The options, in order; a disabled option is shown but cannot be picked.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<OmniOption<TValue>> Options { get; set; } = Array.Empty<OmniOption<TValue>>();

    /// <summary>
    /// Accessible name of the radio group (<c>aria-label</c>). Null names it by the label of an enclosing
    /// <see cref="OmniFormField"/> whose <c>For</c> is <see cref="OmniInputBase{TValue}.Id"/>
    /// (<c>aria-labelledby</c>), else leaves it unnamed.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Whether every option is disabled. Off by default.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>An icon before an option's text (a sun, a moon, a screen); null for none.</summary>
    [Parameter]
    public Func<TValue, OmniIconName?>? IconFor { get; set; }

    private bool IsSelected(TValue value) => EqualityComparer<TValue>.Default.Equals(CurrentValue, value);

    private void SelectOption(OmniOption<TValue> option)
    {
        if (!Disabled && !option.Disabled)
        {
            CurrentValue = option.Value;
        }
    }

    /// <summary>Never parses: the bar sets its value from the option picked, never from text.</summary>
    /// <param name="value">The text, ignored.</param>
    /// <param name="result">Always the default value.</param>
    /// <param name="validationErrorMessage">The localized "invalid selection" message.</param>
    /// <returns>Always false.</returns>
    protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
    {
        result = default!;
        validationErrorMessage = Localize("SelectBarInvalid");
        return false;
    }

    /// <summary>
    /// The chevrons follow the scroll position, which only the browser knows: the shared overflow
    /// script owns them, as it does a tab strip's. A lost circuit is ignored.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _module = await JavaScript.InvokeAsync<IJSObjectReference>("import", Internal.OmniModules.Focus);
                await _module.InvokeVoidAsync("configureSelectBarOverflow", _strip);
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }

    /// <summary>Releases the form subscription, detaches the overflow script and releases its module; a lost circuit is ignored.</summary>
    /// <returns>A task that completes once the script is released.</returns>
    public async ValueTask DisposeAsync()
    {
        // Blazor calls only DisposeAsync on a component that has both: the form subscription of
        // InputBase is released through its own Dispose.
        ((IDisposable)this).Dispose();
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
