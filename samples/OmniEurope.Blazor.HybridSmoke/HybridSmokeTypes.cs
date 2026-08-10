using Microsoft.AspNetCore.Components.WebView.Maui;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.HybridSmoke;

public static class HybridSmokeTypes
{
    public static IReadOnlyList<Type> RequiredTypes { get; } =
    [
        typeof(BlazorWebView),
        typeof(OmniButton)
    ];
}
