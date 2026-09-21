namespace OmniEurope.Blazor.Components;

public partial class OmniTextBox
{
    /// <summary>The input type. Defaults to <see cref="OmniTextBoxType.Text"/>.</summary>
    [Parameter]
    public OmniTextBoxType Type { get; set; } = OmniTextBoxType.Text;

    private string TypeAttribute => Type.ToString().ToLowerInvariant();

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public string? Autocomplete { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    /// <summary>
    /// A decorative icon laid over the start of the field, muted and click-through, such as the
    /// magnifier of a search field: <c>&lt;Icon&gt;&lt;OmniIcon Name="OmniIconName.Search" /&gt;&lt;/Icon&gt;</c>.
    /// The field keeps its own accessible name. Null, the default, renders the input alone.
    /// </summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>
    /// Delay, in milliseconds, between the last keystroke and the value update. Zero, the default,
    /// updates the value on every keystroke. A search field that reloads data on change sets it so the
    /// data reloads once the user pauses instead of once per character, which made the results flicker.
    /// </summary>
    [Parameter]
    public int DebounceMilliseconds { get; set; }

    private CancellationTokenSource? _debounce;

    private async Task HandleInput(ChangeEventArgs args)
    {
        var text = args.Value?.ToString();
        if (DebounceMilliseconds <= 0)
        {
            CurrentValueAsString = text;
            return;
        }

        _debounce?.Cancel();
        _debounce?.Dispose();
        var pending = _debounce = new CancellationTokenSource();
        try
        {
            await Task.Delay(DebounceMilliseconds, pending.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        CurrentValueAsString = text;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _debounce?.Cancel();
            _debounce?.Dispose();
            _debounce = null;
        }

        base.Dispose(disposing);
    }

    protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
    {
        result = value ?? string.Empty;
        validationErrorMessage = null!;
        return true;
    }
}
