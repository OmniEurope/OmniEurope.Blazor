using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The one menu engine of the package, shared by <see cref="Components.OmniOverflowMenu"/>,
/// <see cref="Components.OmniContextMenu"/>, <see cref="Components.OmniSplitButton"/> and
/// <see cref="Components.OmniProfileMenu"/>. It keeps the open state of a menu that the host does not
/// control (<c>Open</c> left null), draws the <c>role="menu"</c> surface with its items, routes the
/// keys of the open menu (arrows, Home and End move, Escape closes and gives the focus back to the
/// trigger, Tab closes and goes on from the trigger) and drives the script of <c>omni-focus.js</c>
/// (<c>openMenu</c>, <c>closeMenu</c>, <c>moveMenuFocus</c>), which places the menu inside the window,
/// focuses its first or last item and reports a press outside.
/// </summary>
internal sealed class OmniMenuController : IAsyncDisposable
{
    private readonly IJSRuntime _javaScript;
    private readonly Func<bool, Task> _dismissAsync;
    private IJSObjectReference? _module;
    private DotNetObjectReference<DismissInterop>? _reference;
    private bool _internalOpen;
    private bool _shown;
    private bool _placementPending;
    private bool _closeRequested;
    private bool _focusLast;
    private double? _x;
    private double? _y;
    private bool _disposed;

    /// <param name="javaScript">The runtime the script is imported through.</param>
    /// <param name="dismissAsync">Closes the menu when the script reports a press outside it; the argument says whether the focus goes back to the trigger.</param>
    internal OmniMenuController(IJSRuntime javaScript, Func<bool, Task> dismissAsync)
    {
        _javaScript = javaScript;
        _dismissAsync = dismissAsync;
    }

    /// <summary>The key the script files this menu under.</summary>
    internal string Key { get; } = $"omni-menu-{Guid.NewGuid():N}";

    /// <summary>Whether the menu is open: <paramref name="controlled"/> when the host sets it, else the menu's own state.</summary>
    internal bool IsOpen(bool? controlled) => controlled ?? _internalOpen;

    /// <summary>
    /// Asks the next render to place the menu and focus an item: the last one with
    /// <paramref name="focusLast"/>, else the first; at (<paramref name="x"/>, <paramref name="y"/>)
    /// for a menu opened at the pointer. Also moves a menu already open.
    /// </summary>
    internal void RequestPlacement(bool focusLast, double? x = null, double? y = null)
    {
        _placementPending = true;
        _closeRequested = false;
        _focusLast = focusLast;
        _x = x;
        _y = y;
    }

    /// <summary>
    /// Opens or closes the menu. Closing gives the script its last word first (so the focus is back
    /// on the trigger when an action chosen in the menu runs); the menu's own state then follows
    /// unless the host controls it, <paramref name="refresh"/> lets the owner re-render and update
    /// its portal entry, and <paramref name="changed"/> is raised when the state changed.
    /// </summary>
    internal async Task SetOpenAsync(bool open, bool? controlled, EventCallback<bool> changed, Action refresh, bool restoreFocus = false)
    {
        if (!open)
        {
            await HideAsync(restoreFocus);

            // A host that controls the menu closes it on its next render; until then the menu is
            // still drawn and must not be handed back to the script.
            _closeRequested = controlled is not null;
        }

        if (open == IsOpen(controlled))
        {
            refresh();
            return;
        }

        if (controlled is null)
        {
            _internalOpen = open;
        }

        refresh();
        await changed.InvokeAsync(open);
    }

    /// <summary>
    /// Called after each render of the owner: opens the script on a menu newly drawn or asked to move,
    /// closes it on a menu the host closed through <c>Open</c>.
    /// </summary>
    internal async Task SyncAsync(bool open, string menuId, ElementReference anchor, OmniMenuPlacement placement, bool closeOnOutsideClick = true)
    {
        if (_disposed)
        {
            return;
        }

        if (!open)
        {
            _closeRequested = false;
        }

        if (open && !_closeRequested && (!_shown || _placementPending))
        {
            _module ??= await _javaScript.InvokeAsync<IJSObjectReference>("import", OmniModules.Focus);
            if (_disposed)
            {
                return;
            }

            _shown = true;
            _placementPending = false;
            _reference ??= DotNetObjectReference.Create(new DismissInterop(_dismissAsync));
            await _module.InvokeVoidAsync(
                "openMenu",
                menuId,
                Key,
                anchor,
                placement.ToString().ToLowerInvariant(),
                _x,
                _y,
                _focusLast,
                closeOnOutsideClick,
                _reference);
        }
        else if (!open && _shown)
        {
            await HideAsync(restoreFocus: false);
        }
    }

    /// <summary>
    /// The keys of the open menu: the arrows, Home and End move between its items, Escape closes it
    /// and gives the focus back to the trigger, Tab closes it and the focus goes on from the trigger.
    /// </summary>
    internal async Task HandleMenuKeyAsync(KeyboardEventArgs args, string menuId, Func<bool, Task> closeAsync)
    {
        switch (args.Key)
        {
            case "ArrowDown" or "ArrowUp" or "Home" or "End":
                if (_module is not null)
                {
                    await _module.InvokeVoidAsync("moveMenuFocus", menuId, args.Key);
                }

                break;
            case "Escape":
                await closeAsync(true);
                break;
            case "Tab":
                await closeAsync(false);
                break;
        }
    }

    /// <summary>
    /// Draws the <c>role="menu"</c> list: named by <paramref name="label"/>, focusable by script only,
    /// its keys routed to <paramref name="onKeyDown"/> and kept from the trigger it may sit in, the
    /// items given <paramref name="menu"/> to close it once chosen.
    /// </summary>
    internal static void BuildMenuList(
        RenderTreeBuilder builder,
        object receiver,
        string menuId,
        string cssClass,
        string label,
        string? density,
        IOmniMenu menu,
        RenderFragment? items,
        Func<KeyboardEventArgs, Task> onKeyDown)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "id", menuId);
        builder.AddAttribute(2, "class", cssClass);
        builder.AddAttribute(3, "role", "menu");
        builder.AddAttribute(4, "aria-label", label);
        builder.AddAttribute(5, "tabindex", "-1");
        // Carried by the menu itself: in the overlay portal it no longer sits inside its component.
        builder.AddAttribute(6, "data-omni-density", density);
        builder.AddAttribute(7, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(receiver, onKeyDown));
        // Drawn in place, the menu sits inside its component: one key must not be handled twice.
        builder.AddEventStopPropagationAttribute(8, "onkeydown", true);
        builder.OpenComponent<CascadingValue<IOmniMenu>>(9);
        builder.AddComponentParameter(10, nameof(CascadingValue<IOmniMenu>.Value), menu);
        builder.AddComponentParameter(11, nameof(CascadingValue<IOmniMenu>.IsFixed), true);
        builder.AddComponentParameter(12, nameof(CascadingValue<IOmniMenu>.ChildContent), items);
        builder.CloseComponent();
        builder.CloseElement();
    }

    /// <summary>Tells the script the menu is gone, once; the focus goes back to the trigger with <paramref name="restoreFocus"/>, or when it was still in the menu.</summary>
    private async Task HideAsync(bool restoreFocus)
    {
        if (!_shown || _module is null)
        {
            return;
        }

        _shown = false;
        try
        {
            await _module.InvokeVoidAsync("closeMenu", Key, restoreFocus);
        }
        catch (JSDisconnectedException)
        {
        }
    }

    /// <summary>Detaches the script from a menu still open and releases the module.</summary>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_module is not null)
        {
            try
            {
                if (_shown)
                {
                    await _module.InvokeVoidAsync("closeMenu", Key, false);
                }

                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        _shown = false;
        _reference?.Dispose();
    }

    /// <summary>What the script calls when a press outside closes the menu.</summary>
    private sealed class DismissInterop(Func<bool, Task> dismissAsync)
    {
        [JSInvokable("OmniMenu.Dismiss")]
        public Task DismissAsync(bool restoreFocus) => dismissAsync(restoreFocus);
    }
}
