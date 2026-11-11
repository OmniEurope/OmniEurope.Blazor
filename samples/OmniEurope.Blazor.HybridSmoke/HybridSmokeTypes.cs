using Microsoft.AspNetCore.Components.WebView.Maui;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.HybridSmoke;

internal static class HybridSmokeTypes
{
    // An array, not a collection expression: CsWinRT (CsWinRT1032, .NET 11 MAUI) rejects a collection
    // expression whose target is a read-only interface, as not marshallable to WinRT.
    internal static IReadOnlyList<Type> RequiredTypes { get; } = new Type[]
    {
        typeof(BlazorWebView),
        typeof(OmniButton)
    };
}
