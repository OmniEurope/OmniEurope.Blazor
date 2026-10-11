using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What <c>omni-preferences.js</c> reports to <see cref="Components.OmniPreferences"/>: the tab became
/// visible again, or another tab of the same browser wrote a preference of the same scope.
/// </summary>
internal sealed class PreferenceInteropBridge(Func<Task> onVisible, Func<string, Task> onStored)
{
    [JSInvokable]
    public Task OnVisible() => onVisible();

    [JSInvokable]
    public Task OnStored(string storageKey) => onStored(storageKey);
}
