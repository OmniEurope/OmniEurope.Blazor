using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What <c>omni-focus.js</c> reports to an <see cref="Components.OmniSubMenu"/> while its list is open:
/// the list that holds the entry scrolled, so the list opened beside the entry no longer faces it.
/// </summary>
internal sealed class SubMenuInteropBridge(Func<Task> scrolledAsync)
{
    [JSInvokable("OmniSubMenu.Scrolled")]
    public Task ScrolledAsync() => scrolledAsync();
}
