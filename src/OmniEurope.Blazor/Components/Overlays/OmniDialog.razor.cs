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
    private string? _appliedWidth;

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
    /// veil to close it. A modal dialog puts the focus on this button when it opens, unless
    /// <see cref="InitialFocus"/> says otherwise.
    /// </summary>
    [Parameter]
    public bool ShowClose { get; set; } = true;

    /// <summary>
    /// Where the focus goes when the dialog opens. <see cref="OmniDialogInitialFocus.CloseButton"/> by
    /// default, as before; <see cref="OmniDialogInitialFocus.Panel"/> focuses the panel itself so that
    /// nothing looks selected until the reader clicks or presses Tab;
    /// <see cref="OmniDialogInitialFocus.FirstFocusable"/> focuses the first field or button of the content.
    /// </summary>
    [Parameter]
    public OmniDialogInitialFocus InitialFocus { get; set; }

    // The script's name of the initial focus; null for the close button keeps the calls it always made.
    private string? InitialFocusTarget => InitialFocus switch
    {
        OmniDialogInitialFocus.Panel => "panel",
        OmniDialogInitialFocus.FirstFocusable => "content",
        _ => null
    };

    // The panel takes the focus itself when it is not dismissible (nothing else may be focusable) or when asked to.
    private string? PanelTabIndex => !Dismissible || InitialFocus == OmniDialogInitialFocus.Panel ? "-1" : null;

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
    /// A free width, in place of <see cref="Size"/>: a positive number followed by <c>px</c>, <c>rem</c>,
    /// <c>em</c>, <c>ch</c>, <c>vw</c> or <c>%</c> (<c>30rem</c>, <c>500px</c>). Like every size it is a
    /// ceiling capped by the viewport, so a narrow screen still gets a full-width dialog. The page's
    /// policy forbids a <c>style</c> attribute, so the dialog script writes it as a custom property once
    /// the dialog is drawn; the dialog stays transparent until then (a second at most, then at the
    /// 40rem width if no script answers). Null, the default, leaves <see cref="Size"/> in charge and
    /// loads no script for it.
    /// </summary>
    /// <exception cref="ArgumentException">The value is not such a length.</exception>
    [Parameter]
    public string? Width { get; set; }

    /// <summary>
    /// What the dialog is for: <see cref="OmniTone.Accent"/> for a form, <see cref="OmniTone.Warning"/>
    /// for a question that is hard to undo, <see cref="OmniTone.Danger"/> for what is lost for good.
    /// The title is led by a round mark, on the tint of the intention, carrying the glyph of the
    /// severity of the same name (the information glyph for Accent), the glyph of
    /// <see cref="OmniAlert"/> and <see cref="OmniNotification"/>; the header and the footer stay
    /// neutral. <see cref="OmniTone.Neutral"/>, the default, draws no mark.
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
    private string? SizeClass => Width is not null ? "omni-dialog--width" : Size switch
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

    /// <summary>Checks <see cref="Width"/> before anything is drawn with it.</summary>
    /// <exception cref="ArgumentException"><see cref="Width"/> is not a number followed by px, rem, em, ch, vw or %.</exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        Internal.DialogWidth.Validate(Width, nameof(Width));
    }

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

        // The free width first: the frozen scale and the focus then see the dialog at its own size.
        await SyncWidthAsync();
        await SyncAttachmentAsync();

        // On Blazor Server a later render can run while the first one still awaits the focus module:
        // it has no module yet and leaves the focus to the first render, which syncs it once loaded.
        if (_focusModule is { } focus)
        {
            await SyncFocusAsync(focus);
        }
    }

    /// <summary>Writes the free width on an open dialog when it changed; a closed dialog forgets it.</summary>
    private async Task SyncWidthAsync()
    {
        if (!Open)
        {
            // Closed, the panel is gone: the next opening draws a new one, without the property.
            _appliedWidth = null;
            return;
        }

        if (string.Equals(Width, _appliedWidth, StringComparison.Ordinal))
        {
            return;
        }

        var module = _dialogModule ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", Internal.OmniModules.Dialog);
        if (Width is null) await module.InvokeVoidAsync("clearWidth", _dialog);
        else await module.InvokeVoidAsync("setWidth", _dialog, Width);
        _appliedWidth = Width;
    }

    /// <summary>Attaches dragging and the frozen scale when the dialog opens with them, detaches them when it closes.</summary>
    private async Task SyncAttachmentAsync()
    {
        if (Open && !_attached && (Draggable || FreezeScale))
        {
            var module = _dialogModule ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", Internal.OmniModules.Dialog);
            await module.InvokeVoidAsync("attach", _dialog);
            if (FreezeScale) await module.InvokeVoidAsync("freezeScale", _dialog);
            _attached = true;
        }
        else if (!Open && _attached)
        {
            // Attached only once the module was loaded.
            await _dialogModule!.InvokeVoidAsync("detach", _dialog);
            _attached = false;
        }
    }

    /// <summary>
    /// Moves focus into the dialog as it opens (a modal traps it, a modeless window lets Tab leave), and
    /// gives it back to where it was when it closes.
    /// </summary>
    private async Task SyncFocusAsync(IJSObjectReference focus)
    {
        if (Open && !_focusActivated)
        {
            _focusActivated = true;

            // A backdrop that closes nothing must not take focus out of the trap either. Only such a
            // dialog passes the flag: a dialog whose backdrop closes it makes the call it always made.
            // The initial focus is passed only when it is not the close button, so a default dialog
            // makes the calls it always made.
            var holdBackdrop = !(CloseOnBackdrop && Dismissible);
            if (!Modal)
            {
                if (InitialFocusTarget is { } target) await focus.InvokeVoidAsync("activateWindow", _dialog, _focusKey, target);
                else await focus.InvokeVoidAsync("activateWindow", _dialog, _focusKey);
            }
            else if (InitialFocusTarget is { } target)
            {
                await focus.InvokeVoidAsync("activateDialog", _dialog, _focusKey, holdBackdrop, target);
            }
            else if (!holdBackdrop)
            {
                await focus.InvokeVoidAsync("activateDialog", _dialog, _focusKey);
            }
            else
            {
                await focus.InvokeVoidAsync("activateDialog", _dialog, _focusKey, true);
            }
        }
        else if (!Open && _focusActivated)
        {
            _focusActivated = false;
            await focus.InvokeVoidAsync("restoreFocus", _focusKey);
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
