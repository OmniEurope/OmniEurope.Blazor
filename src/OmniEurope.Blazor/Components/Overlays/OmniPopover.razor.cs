using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A button that opens an anchored, non-modal panel: a list of running jobs, a short form, a detail
/// card. Unlike <see cref="OmniDialog"/> it does not trap focus or cover the page; unlike a menu it may
/// hold any content. A click outside it or Escape closes it, and Escape returns focus to the trigger.
/// </summary>
public partial class OmniPopover
{
    private const string FocusModulePath = "./_content/OmniEurope.Blazor/omni-focus.js";

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

    /// <summary>Accessible name of the trigger, required when its content is an icon alone.</summary>
    [Parameter]
    public string? TriggerLabel { get; set; }

    [Parameter]
    public OmniButtonVariant TriggerVariant { get; set; } = OmniButtonVariant.Ghost;

    /// <summary>Classes added to the trigger button, for a host that sizes or places it.</summary>
    [Parameter]
    public string? TriggerClass { get; set; }

    /// <summary>Tooltip of the trigger button.</summary>
    [Parameter]
    public string? TriggerTitle { get; set; }

    [Parameter]
    public OmniControlSize TriggerSize { get; set; } = OmniControlSize.Medium;

    /// <summary>Accessible name of the panel, announced when it opens.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public OmniPopoverPlacement Placement { get; set; } = OmniPopoverPlacement.BottomEnd;

    /// <summary>Controls the panel from the host; left unbound, the popover keeps its own state.</summary>
    [Parameter]
    public bool? Open { get; set; }

    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    private bool IsOpen => Open ?? _internalOpen;

    private string PanelId => $"{Id ?? _generatedId}-panel";

    private string PlacementClass => Placement == OmniPopoverPlacement.BottomStart
        ? "omni-popover--bottom-start"
        : "omni-popover--bottom-end";

    private Task ToggleAsync() => SetOpenAsync(!IsOpen, returnFocus: false);

    /// <summary>Closes the panel, as a click outside or Escape does.</summary>
    public Task CloseAsync() => SetOpenAsync(false, returnFocus: true);

    /// <summary>Invoked by the focus script on a click outside the popover or on Escape inside it.</summary>
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
