using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.HybridSmoke.Resources;

namespace OmniEurope.Blazor.HybridSmoke;

public sealed class MainPage : ContentPage
{
    public MainPage(IStringLocalizer<HybridSmokeStrings> text)
    {
        Title = text["WindowTitle"];
        var webView = new BlazorWebView { HostPage = "wwwroot/index.html" };

        // Startup markers: when the self-test never reports, the last marker in the probe log
        // names the stage the host reached.
        webView.BlazorWebViewInitializing += (_, _) => SmokeTrace.Write("webview-initializing");
        webView.BlazorWebViewInitialized += (_, _) => SmokeTrace.Write("webview-initialized");
        webView.UrlLoading += (_, e) => SmokeTrace.Write($"webview-url-loading {e.Url}");
        Loaded += (_, _) => SmokeTrace.Write("page-loaded");

        webView.RootComponents.Add(new RootComponent
        {
            Selector = "#app",
            ComponentType = typeof(HybridSmoke)
        });
        Content = webView;
        SmokeTrace.Write("page-constructed");
    }
}
