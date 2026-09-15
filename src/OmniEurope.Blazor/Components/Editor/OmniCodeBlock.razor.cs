namespace OmniEurope.Blazor.Components;

/// <summary>
/// A read-only block of code, a command or a token, with a button that copies it to the clipboard. A
/// <see cref="Secret"/> is masked on screen (its first and last characters kept, so it can still be told
/// apart) until the reveal button shows it; the copy always takes the full value.
/// </summary>
/// <remarks>
/// The copy goes through <c>omniInterop.js</c>: the asynchronous clipboard API, or a hidden text area
/// and the copy command where it is refused. The outcome is announced through a polite live region and
/// the button shows a check for <see cref="CopiedFeedbackDuration"/>.
/// </remarks>
public partial class OmniCodeBlock : IAsyncDisposable
{
    private const char MaskCharacter = '•';
    private const int MaskLength = 8;

    private OmniClipboardCopy? _clipboard;
    private bool _revealed;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    /// <summary>The code, shown as written.</summary>
    [Parameter, EditorRequired]
    public string Code { get; set; } = string.Empty;

    /// <summary>A caption above the code: what the command does, where it runs.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>The language, shown as a small label: <c>bash</c>, <c>yaml</c>, <c>powershell</c>.</summary>
    [Parameter]
    public string? Language { get; set; }

    /// <summary>A single line, set tighter, for a one-line command.</summary>
    [Parameter]
    public bool Inline { get; set; }

    /// <summary>Whether long lines wrap instead of scrolling sideways.</summary>
    [Parameter]
    public bool Wrap { get; set; }

    /// <summary>Whether the copy button is shown.</summary>
    [Parameter]
    public bool ShowCopy { get; set; } = true;

    /// <summary>Masks the value on screen until the reveal button is pressed; the copy still takes all of it.</summary>
    [Parameter]
    public bool Secret { get; set; }

    /// <summary>How many characters of a secret stay visible at each end while it is masked.</summary>
    [Parameter]
    public int VisibleCharacters { get; set; } = 4;

    /// <summary>How long the button shows the check after a copy.</summary>
    [Parameter]
    public TimeSpan CopiedFeedbackDuration { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>The clock the copy feedback is timed on.</summary>
    [Parameter]
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary>Raised after a copy, with whether the clipboard accepted it.</summary>
    [Parameter]
    public EventCallback<bool> OnCopied { get; set; }

    /// <summary>Whether the secret is shown in clear.</summary>
    public bool IsRevealed => _revealed;

    private OmniClipboardCopy Clipboard => _clipboard ??= new OmniClipboardCopy(JavaScript, () => InvokeAsync(StateHasChanged));

    private string DisplayedCode => Secret && !_revealed ? Mask(Code, VisibleCharacters) : Code;

    private string? CodeClass => Secret && !_revealed ? "omni-code-block__masked" : null;

    private bool? CopyResult => _clipboard?.Result;

    private string CopyLabel => CopyResult switch
    {
        true => Localize("CodeBlockCopied"),
        false => Localize("CodeBlockCopyFailed"),
        null => Localize("CodeBlockCopy")
    };

    private string CopyAnnouncement => CopyResult switch
    {
        true => Localize("CodeBlockCopied"),
        false => Localize("CodeBlockCopyFailed"),
        null => string.Empty
    };

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(TimeProvider);
        ArgumentOutOfRangeException.ThrowIfNegative(VisibleCharacters);
    }

    /// <summary>
    /// Keeps <paramref name="visible"/> characters at each end and replaces the rest by a fixed run of
    /// dots, so the length of the secret does not show either. A value too short to keep both ends
    /// apart is masked whole.
    /// </summary>
    internal static string Mask(string value, int visible)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var dots = new string(MaskCharacter, MaskLength);
        return visible == 0 || value.Length <= (visible * 2) + 2
            ? dots
            : string.Concat(value.AsSpan(0, visible), dots, value.AsSpan(value.Length - visible));
    }

    private void ToggleReveal() => _revealed = !_revealed;

    /// <summary>Copies <see cref="Code"/> to the clipboard; true when the clipboard accepted it.</summary>
    public async Task<bool> CopyAsync()
    {
        var copied = await Clipboard.CopyAsync(Code, CopiedFeedbackDuration, TimeProvider);
        StateHasChanged();
        await OnCopied.InvokeAsync(copied);
        return copied;
    }

    public async ValueTask DisposeAsync()
    {
        if (_clipboard is not null)
        {
            await _clipboard.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
