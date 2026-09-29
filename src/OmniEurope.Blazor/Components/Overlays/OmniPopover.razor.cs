using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A button that opens an anchored, non-modal panel: a list of running jobs, a short form, a detail
/// card. Unlike <see cref="OmniDialog"/> it does not trap focus or cover the page; unlike a menu it may
/// hold any content. A click outside it or Escape closes it, and Escape returns focus to the trigger.
/// </summary>
public partial class OmniPopover
{
    private const string FocusModulePath = Internal.OmniModules.Focus;

    private readonly string _generatedId = $"omni-popover-{Guid.NewGuid():N}";
    private ElementReference _root;
    private ElementReference _panel;
    private IJSObjectReference? _module;
    private DotNetObjectReference<OmniPopover>? _selfReference;
    private bool _internalOpen;
    private bool _panelActive;
    private bool _restoreFocusOnClose;

    /// <summary>Content of the trigger button: text, an icon, a counter.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? TriggerContent { get; set; }

    /// <summary>
    /// Accessible name of the trigger, required when its content is an icon alone; also the tooltip of
    /// the trigger and the name of the panel when <see cref="PopupLabel"/> is null. Null, the default,
    /// leaves the trigger named by its content.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>The emphasis of the trigger button. <see cref="OmniButtonVariant.Ghost"/> by default.</summary>
    [Parameter]
    public OmniButtonVariant TriggerVariant { get; set; } = OmniButtonVariant.Ghost;

    /// <summary>Classes added to the trigger button, for a host that sizes or places it.</summary>
    [Parameter]
    public string? TriggerClass { get; set; }

    /// <summary>
    /// Tooltip of the trigger button. Null, the default, repeats <see cref="Label"/>, which is
    /// what an icon-only trigger shows on hover.
    /// </summary>
    [Parameter]
    public string? TriggerTitle { get; set; }

    /// <summary>
    /// Accessible name of the panel, announced when it opens. Null, the default, repeats
    /// <see cref="Label"/>; without either the panel is named by its trigger.
    /// </summary>
    [Parameter]
    public string? PopupLabel { get; set; }

    /// <summary>The content of the panel: any content, focusable or not.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Which edge of the trigger the panel aligns to. <see cref="OmniPopoverPlacement.BottomEnd"/> by default.</summary>
    [Parameter]
    public OmniPopoverPlacement Placement { get; set; } = OmniPopoverPlacement.BottomEnd;

    /// <summary>Size of the trigger, the three sizes of <see cref="OmniButton"/>.</summary>
    [Parameter]
    public OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    /// <summary>Disables the trigger: the panel does not open.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Controls the panel from the host; left unbound, the popover keeps its own state.</summary>
    [Parameter]
    public bool? Open { get; set; }

    /// <summary>Raised with the new state when the panel opens or closes, whether the host controls it or not.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    private bool IsOpen => Open ?? _internalOpen;

    private string PanelId => $"{TriggerId}-panel";

    private string TriggerId => Id ?? _generatedId;

    private string? EffectivePopupLabel => string.IsNullOrWhiteSpace(PopupLabel)
        ? string.IsNullOrWhiteSpace(Label) ? null : Label
        : PopupLabel;

    private string PlacementClass => Placement == OmniPopoverPlacement.BottomStart
        ? "omni-popover--bottom-start"
        : "omni-popover--bottom-end";

    private Task ToggleAsync() => Disabled && !IsOpen ? Task.CompletedTask : SetOpenAsync(!IsOpen, returnFocus: false);

    /// <summary>Closes the panel, as a click outside or Escape does.</summary>
    public Task CloseAsync() => SetOpenAsync(false, returnFocus: true);

    /// <summary>Invoked by the focus script on a click outside the popover or on Escape inside it.</summary>
    /// <param name="fromKeyboard">True for Escape, which gives the focus back to the trigger.</param>
    [JSInvokable]
    public Task OnDismissRequestedAsync(bool fromKeyboard) => SetOpenAsync(false, fromKeyboard);

    private async Task SetOpenAsync(bool open, bool returnFocus)
    {
        if (open == IsOpen)
        {
            return;
        }

        if (Open is null)
        {
            _internalOpen = open;
        }

        // The panel goes first; focus comes back after the render. Restoring focus waits for animation
        // frames, which a background tab never delivers: awaiting it here left the panel on screen.
        _restoreFocusOnClose = !open && returnFocus;
        StateHasChanged();
        await OpenChanged.InvokeAsync(open);
    }

    /// <summary>
    /// When the panel opens, attaches the script that closes it on a click outside or on Escape. When it
    /// closes, detaches that script and, when closed by Escape or <see cref="CloseAsync"/>, gives focus
    /// back to the trigger.
    /// </summary>
    /// <param name="firstRender">True on the first render of the component.</param>
    /// <returns>A task that completes once the script calls are done.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsOpen && !_panelActive)
        {
            _module ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", FocusModulePath);
            _selfReference ??= DotNetObjectReference.Create(this);
            _panelActive = true;
            await _module.InvokeVoidAsync("attachPopover", _root, _panel, _selfReference, PanelId);
        }
        else if (!IsOpen && _panelActive)
        {
            _panelActive = false;
            var restore = _restoreFocusOnClose;
            _restoreFocusOnClose = false;
            await DeactivateAsync(restore);
        }
    }

    private async Task DeactivateAsync(bool restore)
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("detachPopover", PanelId, restore);
        }
        catch (JSDisconnectedException)
        {
        }
    }

    /// <summary>Detaches the script from an open panel and releases the module.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("detachPopover", PanelId, false);
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        _selfReference?.Dispose();
    }
}
