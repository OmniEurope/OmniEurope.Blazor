namespace OmniEurope.Blazor.Components;

/// <summary>
/// A dialog: a titled panel over a veil that makes the page inert and traps the focus (modal, the
/// default), or a modeless window with <see cref="Modal"/> off. It closes by its close button,
/// Escape and a press on the veil while <see cref="Dismissible"/>, and gives the focus back to what
/// had it when it opened.
/// </summary>
public partial class OmniDialog
{
    private readonly string _focusKey = $"dialog-{Guid.NewGuid():N}";
    private readonly string _generatedId = $"omni-dialog-{Guid.NewGuid():N}";
    private ElementReference _dialog;
    private IJSObjectReference? _focusModule;
    private IJSObjectReference? _dialogModule;
    private bool _focusActivated;
    private bool _attached;

    /// <summary>False opens a modeless window: no veil, inert page, or focus trap.</summary>
    [Parameter] public bool Modal { get; set; } = true;

    /// <summary>Capture the current text scale and density until this window closes.</summary>
    [Parameter] public bool FreezeScale { get; set; }

    /// <summary>Whether the dialog is shown; the host owns it (<c>@bind-Open</c>).</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Raised with false when the reader closes the dialog (close button, Escape, veil).</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>The title, the heading that names the dialog.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// What the title heading shows in place of <see cref="Title"/> (an icon next to the text, a
    /// badge). It stays inside the heading that names the dialog, so it must carry readable text.
    /// Null shows <see cref="Title"/>, as before.
    /// </summary>
    [Parameter]
    public RenderFragment? TitleContent { get; set; }

    /// <summary>Accessible name and tooltip of the close button. Null, the default, is the localized "Close".</summary>
    [Parameter]
    public string? CloseLabel { get; set; }

    private string EffectiveCloseLabel => LocalizeOr(CloseLabel, "Close");

    /// <summary>Whether a press on the veil closes a dismissible dialog. True by default.</summary>
    [Parameter]
    public bool CloseOnBackdrop { get; set; } = true;

    /// <summary>
    /// Whether the reader can dismiss the dialog. True by default: the close button, Escape and the
    /// veil (see <see cref="CloseOnBackdrop"/>) close it. False removes the close button, ignores
    /// Escape and the veil, and announces the panel as an <c>alertdialog</c> described by its content:
    /// only the host, through <see cref="Open"/>, closes it. Focus stays trapped inside a modal one;
    /// with nothing focusable in its content, the panel itself takes the focus.
    /// </summary>
    [Parameter]
    public bool Dismissible { get; set; } = true;

    /// <summary>
    /// Draws the close button of a dismissible dialog. True by default; false leaves Escape and the
    /// veil to close it. A modal dialog puts the focus on this button when it opens.
    /// </summary>
    [Parameter]
    public bool ShowClose { get; set; } = true;

    /// <summary>Lets the reader move the dialog by its header.</summary>
    [Parameter]
    public bool Draggable { get; set; }

    /// <summary>Lets the reader resize the dialog by its corner.</summary>
    [Parameter]
    public bool Resizable { get; set; }

    /// <summary>
    /// How wide the dialog may grow. <see cref="OmniDialogSize.Medium"/> by default, the 40rem it
    /// always had; every size stays capped by the viewport, and full width below a 40rem viewport.
    /// </summary>
    [Parameter]
    public OmniDialogSize Size { get; set; }

    /// <summary>
    /// What the dialog is for: <see cref="OmniTone.Accent"/> for a form, <see cref="OmniTone.Warning"/>
    /// for a question that is hard to undo, <see cref="OmniTone.Danger"/> for what is lost for good.
    /// The header and the footer take its tint and the title is led by a round mark carrying the glyph
    /// of the severity of the same name (the information glyph for Accent), the glyph of
    /// <see cref="OmniAlert"/> and <see cref="OmniNotification"/>. <see cref="OmniTone.Neutral"/>,
    /// the default, draws no tint and no mark.
    /// </summary>
    [Parameter]
    public OmniTone Intent { get; set; } = OmniTone.Neutral;

    /// <summary>
    /// The icon of the intention mark, in place of the glyph <see cref="Intent"/> brings; decorative,
    /// the title names the dialog. Shown only with an intention other than <see cref="OmniTone.Neutral"/>:
    /// without one there is no mark to hold it.
    /// </summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>The body of the dialog.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The footer, in general the action buttons, at its end. Null, the default, draws no footer.</summary>
    [Parameter]
    public RenderFragment? Footer { get; set; }

    private string EffectiveId => Id ?? _generatedId;
    private string TitleId => $"{EffectiveId}-title";
    private string ContentId => $"{EffectiveId}-content";

    // Medium carries no modifier: the base rule is its width, so a dialog that never set a size
    // renders exactly the classes it always had.
    private string? SizeClass => Size switch
    {
        OmniDialogSize.Small => "omni-dialog--small",
        OmniDialogSize.Large => "omni-dialog--large",
        OmniDialogSize.ExtraLarge => "omni-dialog--xlarge",
        OmniDialogSize.FullWidth => "omni-dialog--full",
        _ => null
    };

    // Neutral carries no modifier either: a dialog without an intention keeps its markup and its classes.
    private string? IntentClass => Intent == OmniTone.Neutral
        ? null
        : $"omni-dialog--intent-{Intent.ToString().ToLowerInvariant()}";

    private string IntentGlyph => OmniSeverityGlyph.For(Intent);

    private async Task CloseAsync()
    {
        await OpenChanged.InvokeAsync(false);
        if (_focusModule is not null)
        {
            await _focusModule.InvokeVoidAsync("restoreFocus", _focusKey);
        }
    }

    /// <summary>
    /// Loads the focus script on the first render. When the dialog opens, attaches dragging and the frozen
    /// scale if asked for, and moves focus into it: a modal traps it, a modeless window lets Tab leave.
    /// When it closes, detaches them and gives focus back to where it was before the dialog opened.
    /// </summary>
    /// <param name="firstRender">True on the first render of the component.</param>
    /// <returns>A task that completes once the script calls are done.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _focusModule = await JavaScript.InvokeAsync<IJSObjectReference>("import", Internal.OmniModules.Focus);
        }

        if (Open && !_attached && (Draggable || FreezeScale))
        {
            _dialogModule ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", Internal.OmniModules.Dialog);
            await _dialogModule.InvokeVoidAsync("attach", _dialog);
            if (FreezeScale) await _dialogModule.InvokeVoidAsync("freezeScale", _dialog);
            _attached = true;
        }
        else if (!Open && _attached)
        {
            if (_dialogModule is not null) await _dialogModule.InvokeVoidAsync("detach", _dialog);
            _attached = false;
        }

        if (_focusModule is null)
        {
            return;
        }

        if (Open && !_focusActivated)
        {
            _focusActivated = true;

            // A backdrop that closes nothing must not take focus out of the trap either. Only such a
            // dialog passes the flag: a dialog whose backdrop closes it makes the call it always made.
            if (!Modal)
            {
                await _focusModule.InvokeVoidAsync("activateWindow", _dialog, _focusKey);
            }
            else if (CloseOnBackdrop && Dismissible)
            {
                await _focusModule.InvokeVoidAsync("activateDialog", _dialog, _focusKey);
            }
            else
            {
                await _focusModule.InvokeVoidAsync("activateDialog", _dialog, _focusKey, true);
            }
        }
        else if (!Open && _focusActivated)
        {
            _focusActivated = false;
            await _focusModule.InvokeVoidAsync("restoreFocus", _focusKey);
        }
    }

    private Task HandleBackdropAsync() => CloseOnBackdrop && Dismissible ? CloseAsync() : Task.CompletedTask;
    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key == "Escape" && Dismissible)
        {
            await CloseAsync();
        }
    }

    private Task FocusFirstAsync(FocusEventArgs _) => FocusBoundaryAsync(last: false);
    private Task FocusLastAsync(FocusEventArgs _) => FocusBoundaryAsync(last: true);

    private async Task FocusBoundaryAsync(bool last)
    {
        if (_focusModule is not null)
        {
            await _focusModule.InvokeVoidAsync("focusBoundary", _dialog, last);
        }

    }

    /// <summary>Gives the focus back and detaches the scripts of a dialog still open.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_focusModule is not null)
        {
            try
            {
                await _focusModule.InvokeVoidAsync("restoreFocus", _focusKey);
                await _focusModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        if (_dialogModule is not null)
        {
            try
            {
                await _dialogModule.InvokeVoidAsync("detach", _dialog);
                await _dialogModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        GC.SuppressFinalize(this);
    }
}
