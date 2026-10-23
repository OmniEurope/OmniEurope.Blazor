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

    /// <summary>The code, shown as written; null is shown as empty.</summary>
    [Parameter, EditorRequired]
    public string? Code { get; set; }

    /// <summary>A caption above the code: what the command does, where it runs. Nothing is drawn when null.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>The language identifier (<c>bash</c>, <c>yaml</c>, <c>powershell</c>), as on <see cref="OmniCodeEditor"/>; shown as a small label in the header.</summary>
    [Parameter]
    public string? Language { get; set; }

    /// <summary>Whether the code is a single line set inside running text, tighter, for a one-line command.</summary>
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

    /// <summary>
    /// What the code does with the ANSI escape sequences of a command output (<c>ESC [ 32 m</c>):
    /// <see cref="OmniAnsiMode.Render"/> (the default, as on <see cref="OmniLogViewer.AnsiMode"/>) draws the colours and
    /// attributes, <see cref="OmniAnsiMode.Hide"/> removes the sequences without drawing anything, <see cref="OmniAnsiMode.Raw"/>
    /// shows the code as written. Code without a sequence is unchanged in every mode. The copy takes the code without its
    /// sequences in <see cref="OmniAnsiMode.Render"/> and <see cref="OmniAnsiMode.Hide"/>, and as written in <see cref="OmniAnsiMode.Raw"/>.
    /// </summary>
    [Parameter]
    public OmniAnsiMode AnsiMode { get; set; } = OmniAnsiMode.Render;

    /// <summary>How long the button shows the check after a copy.</summary>
    [Parameter]
    public TimeSpan CopiedFeedbackDuration { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Raised after a copy, with whether the clipboard accepted it.</summary>
    [Parameter]
    public EventCallback<bool> OnCopy { get; set; }

    /// <summary>Whether the secret is shown in clear.</summary>
    public bool IsRevealed => _revealed;

    private OmniClipboardCopy Clipboard => _clipboard ??= new OmniClipboardCopy(JavaScript, () => InvokeAsync(StateHasChanged));

    /// <summary>The text a reader sees and the copy takes: the code without its sequences, or as written in Raw.</summary>
    private string CopiedText => OmniAnsiMarkup.DisplayText(Code, AnsiMode);

    private bool Masked => Secret && !_revealed;

    /// <summary>The code as drawn: the mask of a hidden secret, else the code read for <see cref="AnsiMode"/>.</summary>
    private RenderFragment CodeContent => Masked
        ? builder => builder.AddContent(0, Mask(CopiedText, VisibleCharacters))
        : OmniAnsiMarkup.Fragment(Code, AnsiMode, null);

    private string? CodeClass => Masked ? "omni-code-block__masked" : null;

    private bool? CopyResult => _clipboard?.Result;

    private string CopyLabel => Localize(OmniClipboardCopy.LabelKey(CopyResult));

    private string RevealLabel => Localize(_revealed ? "CodeBlockHide" : "CodeBlockReveal");

    /// <summary>The header shared with <see cref="OmniCodeViewer"/>, with the reveal button of a secret before the copy.</summary>
    private RenderFragment Header => builder =>
    {
        builder.OpenComponent<CodeHeader>(0);
        builder.AddComponentParameter(1, nameof(CodeHeader.Block), "omni-code-block");
        builder.AddComponentParameter(2, nameof(CodeHeader.Element), "figcaption");
        builder.AddComponentParameter(3, nameof(CodeHeader.Title), Title);
        builder.AddComponentParameter(4, nameof(CodeHeader.Language), Language);
        builder.AddComponentParameter(5, nameof(CodeHeader.ShowCopy), ShowCopy);
        builder.AddComponentParameter(6, nameof(CodeHeader.CopyResult), CopyResult);
        builder.AddComponentParameter(7, nameof(CodeHeader.CopyLabel), CopyLabel);
        builder.AddComponentParameter(8, nameof(CodeHeader.OnCopy), EventCallback.Factory.Create<MouseEventArgs>(this, CopyAsync));
        if (Secret)
        {
            builder.AddComponentParameter(9, nameof(CodeHeader.LeadingActions), RevealButton);
        }

        builder.CloseComponent();
    };

    private RenderFragment RevealButton => builder =>
    {
        builder.OpenComponent<OmniButton>(0);
        builder.AddComponentParameter(1, nameof(OmniButton.Class), "omni-code-block__reveal");
        builder.AddComponentParameter(2, nameof(OmniButton.Variant), OmniButtonVariant.Ghost);
        builder.AddComponentParameter(3, nameof(OmniButton.Size), OmniControlSize.Small);
        builder.AddComponentParameter(4, nameof(OmniButton.Label), RevealLabel);
        builder.AddComponentParameter(5, "aria-pressed", _revealed ? "true" : "false");
        builder.AddComponentParameter(6, "title", RevealLabel);
        builder.AddComponentParameter(7, nameof(OmniButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, ToggleReveal));
        builder.AddComponentParameter(8, nameof(OmniButton.Icon), (RenderFragment)(icon =>
        {
            icon.OpenComponent<OmniIcon>(0);
            icon.AddComponentParameter(1, nameof(OmniIcon.Name), _revealed ? OmniIconName.EyeSlash : OmniIconName.Eye);
            icon.AddComponentParameter(2, nameof(OmniIcon.Size), OmniControlSize.Small);
            icon.CloseComponent();
        }));
        builder.CloseComponent();
    };

    private string CopyAnnouncement => OmniClipboardCopy.AnnouncementKey(CopyResult) is { } key ? Localize(key) : string.Empty;

    /// <summary>Checks the parameters.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="VisibleCharacters"/> is negative.</exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
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

    /// <summary>Copies <see cref="Code"/> to the clipboard, without its ANSI sequences unless <see cref="AnsiMode"/> is <see cref="OmniAnsiMode.Raw"/>; true when the clipboard accepted it.</summary>
    public async Task<bool> CopyAsync()
    {
        var copied = await Clipboard.CopyAsync(CopiedText, CopiedFeedbackDuration, Clock);
        StateHasChanged();
        await OnCopy.InvokeAsync(copied);
        return copied;
    }

    /// <summary>Releases the clipboard helper and its pending feedback, when a copy created one.</summary>
    /// <returns>A task that completes once the helper is released.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_clipboard is not null)
        {
            await _clipboard.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
