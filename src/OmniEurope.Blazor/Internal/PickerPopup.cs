namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The panel of a date, time or date and time picker: whether it is open, and its wiring to
/// <c>omni-focus.js</c>. The script closes the panel on a press outside it or on Escape, keeps a single
/// picker panel open on the page, stops the arrow and page keys from scrolling the page while a grid
/// or a column has the focus, and gives the focus back to the toggle when the panel closes from the
/// keyboard. The pickers share this class so that the three behave the same way.
/// </summary>
internal sealed class PickerPopup(IJSRuntime javaScript, Func<bool, Task> dismissed)
{
    private const string FocusModulePath = "./_content/OmniEurope.Blazor/omni-focus.js";

    private IJSObjectReference? _module;
    private DotNetObjectReference<PickerPopupBridge>? _bridge;
    private bool _attached;
    private bool _restoreOnClose;
    private string? _focusSelector;

    internal string Key { get; } = $"omni-picker-{Guid.NewGuid():N}";

    internal bool IsOpen { get; private set; }

    /// <summary>Opens the panel; after the render, the focus goes to the element the selector names.</summary>
    internal void Open(string focusSelector)
    {
        IsOpen = true;
        _focusSelector = focusSelector;
    }

    /// <summary>Closes the panel; <paramref name="restoreFocus"/> gives the focus back to the toggle.</summary>
    internal void Close(bool restoreFocus)
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        _focusSelector = null;
        _restoreOnClose = restoreFocus;
    }

    /// <summary>Moves the focus, after the next render, to the element the selector names in the panel.</summary>
    internal void FocusAfterRender(string selector) => _focusSelector = selector;

    internal async Task AfterRenderAsync(ElementReference root, ElementReference panel, ElementReference toggle)
    {
        if (IsOpen && !_attached)
        {
            _module ??= await javaScript.InvokeAsync<IJSObjectReference>("import", FocusModulePath);
            _bridge ??= DotNetObjectReference.Create(new PickerPopupBridge(dismissed));
            _attached = true;
            await _module.InvokeVoidAsync("attachPicker", root, panel, toggle, _bridge, Key);
        }
        else if (!IsOpen && _attached)
        {
            _attached = false;
            var restore = _restoreOnClose;
            _restoreOnClose = false;
            await _module!.InvokeVoidAsync("detachPicker", Key, restore);
            return;
        }

        if (IsOpen && _focusSelector is { } selector && _module is not null)
        {
            _focusSelector = null;
            await _module.InvokeVoidAsync("focusPickerItem", panel, selector);
        }
    }

    /// <summary>
    /// Releases the listeners of a panel still open when its picker leaves the page. Called from a
    /// synchronous <c>Dispose</c>, so it only starts the release and never throws.
    /// </summary>
    internal void Release()
    {
        var module = _module;
        var bridge = _bridge;
        var attached = _attached;
        _module = null;
        _bridge = null;
        _attached = false;
        _ = ReleaseAsync(module, bridge, attached, Key);
    }

    private static async Task ReleaseAsync(IJSObjectReference? module, DotNetObjectReference<PickerPopupBridge>? bridge, bool attached, string key)
    {
        try
        {
            if (module is not null)
            {
                if (attached)
                {
                    await module.InvokeVoidAsync("detachPicker", key, false);
                }

                await module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the document with it.
        }
        catch (ObjectDisposedException)
        {
            // The runtime was disposed before the picker.
        }
        catch (TaskCanceledException)
        {
            // The page unloaded during the call.
        }
        finally
        {
            bridge?.Dispose();
        }
    }
}

/// <summary>
/// The only .NET object the picker script can call. Kept apart from the pickers so that the callable
/// surface is internal rather than public API.
/// </summary>
internal sealed class PickerPopupBridge(Func<bool, Task> dismissed)
{
    /// <summary>A press outside the picker (<c>false</c>) or Escape inside it (<c>true</c>).</summary>
    [JSInvokable]
    public Task OnDismissRequestedAsync(bool fromKeyboard) => dismissed(fromKeyboard);
}
