using Microsoft.AspNetCore.Components.WebView.Maui;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.HybridSmoke;

internal static class HybridSmokeTypes
{
    internal static IReadOnlyList<Type> RequiredTypes { get; } =
    [
        typeof(BlazorWebView),
        typeof(OmniButton)
    ];
}
