namespace OmniEurope.Blazor.Components;

/// <summary>
/// A single-line text field bound to a <see cref="string"/>, with an optional leading icon and an
/// optional debounce. <see cref="OmniInputBase{TValue}.Class"/> goes on the outermost element (the input,
/// or the wrapper when <see cref="Icon"/> is set); <see cref="OmniInputBase{TValue}.Id"/> and the
/// additional attributes always go on the input.
/// </summary>
public partial class OmniTextBox
{
    /// <summary>The input type. Defaults to <see cref="OmniTextBoxType.Text"/>.</summary>
    [Parameter]
    public OmniTextBoxType Type { get; set; } = OmniTextBoxType.Text;

    private string TypeAttribute => Type.ToString().ToLowerInvariant();

    /// <summary>A hint shown while the field is empty (<c>placeholder</c>); none by default. Not a label.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    /// The browser autofill hint (<c>autocomplete</c>), such as <c>email</c>, <c>username</c> or <c>off</c>.
    /// Null, the default, writes no attribute and leaves the browser's own behaviour.
    /// </summary>
    [Parameter]
    public string? Autocomplete { get; set; }

    /// <summary>Disables the field: it cannot be focused, edited or submitted.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Shows the value without letting it change; the field stays focusable and is submitted.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>The id of the element that describes the field (<c>aria-describedby</c>).</summary>
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
    /// Delay between the last keystroke and the value update. <see cref="TimeSpan.Zero"/>, the default
    /// (and any negative value), updates the value on every keystroke. A search field that reloads data on
    /// change sets it so the data reloads once the user pauses instead of once per character, which made
    /// the results flicker. The text typed during the delay is not lost: leaving the field, or submitting
    /// the form it belongs to, updates the value at once (before the form validates), and a field removed
    /// from the page during the delay hands it to the bound value as it is disposed.
    /// </summary>
    [Parameter]
    public TimeSpan Debounce { get; set; }

    /// <summary>
    /// Welds a Copy button to the end of the field, which puts the value on the clipboard: a URL to
    /// clone, a key, an identifier, usually with <see cref="ReadOnly"/>. The button keeps its word
    /// "Copy" and shows a check after a copy for <see cref="CopiedFeedbackDuration"/>; the outcome
    /// ("Copied", or "Copy failed") is announced to assistive technologies. It is disabled while the
    /// field is empty or disabled. Off by default.
    /// </summary>
    [Parameter]
    public bool Copyable { get; set; }

    /// <summary>How long the Copy button of <see cref="Copyable"/> shows the outcome of a copy.</summary>
    [Parameter]
    public TimeSpan CopiedFeedbackDuration { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Raised after a copy by the <see cref="Copyable"/> button, with whether the clipboard accepted it.</summary>
    [Parameter]
    public EventCallback<bool> OnCopy { get; set; }

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    private OmniClipboardCopy? _clipboard;
    private CancellationTokenSource? _debounce;
    private string? _pendingText;
    private bool _hasPendingText;
    private EditContext? _flushedEditContext;

    // No blur handler at all without a debounce: a field that does not ask for one renders as before.
    private EventCallback<FocusEventArgs> BlurCallback => Debounce > TimeSpan.Zero
        ? EventCallback.Factory.Create<FocusEventArgs>(this, FlushPendingText)
        : default;

    /// <summary>
    /// Follows the form's validation requests, so a submit takes the text still in the delay. An input
    /// keeps the form it started in (InputBase refuses another), so it subscribes once.
    /// </summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (_flushedEditContext is null && EditContext is { } form)
        {
            _flushedEditContext = form;
            form.OnValidationRequested += FlushOnValidationRequested;
        }
    }

    private async Task HandleInput(ChangeEventArgs args)
    {
        var text = args.Value?.ToString();
        if (Debounce <= TimeSpan.Zero)
        {
            CurrentValueAsString = text;
            return;
        }

        _pendingText = text;
        _hasPendingText = true;
        _debounce?.Cancel();
        _debounce?.Dispose();
        var pending = _debounce = new CancellationTokenSource();
        try
        {
            await Task.Delay(Debounce, pending.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        FlushPendingText();
    }

    private void FlushOnValidationRequested(object? sender, ValidationRequestedEventArgs args) => FlushPendingText();

    private OmniClipboardCopy Clipboard => _clipboard ??= new OmniClipboardCopy(JavaScript, () => InvokeAsync(StateHasChanged));

    private bool? CopyResult => _clipboard?.Result;

    // The button keeps its word and its size; the icon shows the outcome and the live region says it.
    private string CopyLabel => Localize(OmniClipboardCopy.LabelKey(null));

    private string CopyAnnouncement => OmniClipboardCopy.AnnouncementKey(CopyResult) is { } key ? Localize(key) : string.Empty;

    // Copies the value as the field shows it, the text still in the debounce delay included.
    private async Task CopyAsync()
    {
        var text = _hasPendingText ? _pendingText ?? string.Empty : CurrentValueAsString ?? string.Empty;
        var copied = await Clipboard.CopyAsync(text, CopiedFeedbackDuration, Clock);
        await OnCopy.InvokeAsync(copied);
    }

    /// <summary>Releases the clipboard module of <see cref="Copyable"/> and disposes the input.</summary>
    public async ValueTask DisposeAsync()
    {
        // Blazor calls only DisposeAsync on a component that has both: the debounce flush and the form
        // subscription are released through Dispose.
        ((IDisposable)this).Dispose();
        if (_clipboard is not null)
        {
            await _clipboard.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    // Takes the text still waiting for the end of the delay, now; nothing when none is waiting.
    private void FlushPendingText()
    {
        if (!_hasPendingText)
        {
            return;
        }

        _hasPendingText = false;
        _debounce?.Cancel();
        _debounce?.Dispose();
        _debounce = null;
        CurrentValueAsString = _pendingText;
    }

    /// <summary>
    /// Hands a text still waiting for the end of the delay to the bound value, leaves the form's
    /// validation requests and disposes the input.
    /// </summary>
    /// <remarks>
    /// The renderer disposes a removed field on its dispatcher while it processes the batch that removed
    /// it: the text reaches <see cref="InputBase{TValue}.ValueChanged"/> and the edit context there, and
    /// the render the parent then asks for joins that batch. A parent removed in the same batch, or a
    /// renderer being disposed, ignores that render request: the value callback still runs, and nothing
    /// renders the disposed field again. An exception thrown by the host's own value callback propagates
    /// as it does on any keystroke.
    /// </remarks>
    /// <param name="disposing">True when called from <see cref="IDisposable.Dispose"/>.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FlushPendingText();
            if (_flushedEditContext is not null)
            {
                _flushedEditContext.OnValidationRequested -= FlushOnValidationRequested;
                _flushedEditContext = null;
            }
        }

        base.Dispose(disposing);
    }

    /// <summary>Takes the text as it is, null as an empty string; never fails.</summary>
    protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
    {
        result = value ?? string.Empty;
        validationErrorMessage = null!;
        return true;
    }
}
