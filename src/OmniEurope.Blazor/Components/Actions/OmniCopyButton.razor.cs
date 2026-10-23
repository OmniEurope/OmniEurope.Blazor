namespace OmniEurope.Blazor.Components;

/// <summary>
/// A button that puts a text on the clipboard: <see cref="Text"/>, or the result of
/// <see cref="TextFactory"/> computed at the moment of the click. It says "copied" for
/// <see cref="CopiedFeedbackDuration"/>, then goes back to its label; a clipboard the browser refuses is
/// announced politely, never thrown.
/// </summary>
/// <remarks>
/// The copy goes through <c>omniInterop.js</c> like <see cref="OmniCodeBlock"/>: the asynchronous
/// clipboard API, then a hidden text area and the copy command where it is refused. When both
/// <see cref="Text"/> and <see cref="TextFactory"/> are set, the factory wins (the text is only the
/// fallback for markup that has nothing to compute). With neither, or with an empty text and no factory,
/// the button is disabled. An exception thrown by the factory is the host's own and is not swallowed.
/// </remarks>
public partial class OmniCopyButton : IAsyncDisposable
{
    private OmniClipboardCopy? _clipboard;
    private bool _working;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    /// <summary>The text to copy. Ignored when <see cref="TextFactory"/> is set.</summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>
    /// Computes the text at the moment of the click, for a text that is costly or changes (an error report
    /// written from the current state). While it runs the button is busy. When set, it wins over <see cref="Text"/>;
    /// a null result is copied as an empty text.
    /// </summary>
    [Parameter]
    public Func<Task<string>>? TextFactory { get; set; }

    /// <summary>
    /// The words of the button, and its accessible name. Null, the default, gives the localized "Copy".
    /// While the feedback lasts the button says "Copied", or "Copy failed", instead.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>What the button means, which sets its colours. <see cref="OmniButtonVariant.Secondary"/> by default.</summary>
    [Parameter]
    public OmniButtonVariant Variant { get; set; } = OmniButtonVariant.Secondary;

    /// <summary>The height, padding and text size. <see cref="OmniControlSize.Medium"/> by default.</summary>
    [Parameter]
    public OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    /// <summary>
    /// The icon drawn before the words, in general an <see cref="OmniIcon"/>. Null, the default, draws the
    /// copy icon, which becomes a check while the "copied" feedback lasts; an icon given here is kept as is.
    /// </summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>Disables the button.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>How long the button says "copied" (or "copy failed") after a copy, two seconds by default.</summary>
    [Parameter]
    public TimeSpan CopiedFeedbackDuration { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Raised after a copy, with whether the clipboard accepted the text.</summary>
    [Parameter]
    public EventCallback<bool> OnCopy { get; set; }

    private OmniClipboardCopy Clipboard => _clipboard ??= new OmniClipboardCopy(JavaScript, () => InvokeAsync(StateHasChanged));

    private bool? CopyResult => _clipboard?.Result;

    private bool CanCopy => TextFactory is not null || !string.IsNullOrEmpty(Text);

    private string ButtonText => CopyResult switch
    {
        true => Localize("CopyButtonCopied"),
        false => Localize("CodeBlockCopyFailed"),
        null => LocalizeOr(Label, "CodeBlockCopy")
    };

    private string Announcement => CopyResult is null ? string.Empty : ButtonText;

    private RenderFragment IconContent => Icon ?? (builder =>
    {
        builder.OpenComponent<OmniIcon>(0);
        builder.AddComponentParameter(1, nameof(OmniIcon.Name), CopyResult == true ? OmniIconName.Check : OmniIconName.Copy);
        builder.CloseComponent();
    });

    private async Task CopyAsync()
    {
        string text;
        if (TextFactory is { } factory)
        {
            _working = true;
            try
            {
                text = await factory() ?? string.Empty;
            }
            finally
            {
                _working = false;
            }
        }
        else
        {
            text = Text ?? string.Empty;
        }

        var copied = await Clipboard.CopyAsync(text, CopiedFeedbackDuration, Clock);
        await OnCopy.InvokeAsync(copied);
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
