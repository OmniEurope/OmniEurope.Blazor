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

        // The smoke probe drives this host through the WebView2 debugging port. When that port
        // never opens, the probe cannot tell a WebView2 that failed to start from one that
        // started without honouring the port. These markers make the runner log say which.
        webView.BlazorWebViewInitializing += (_, _) => Trace("webview-initializing");
        webView.BlazorWebViewInitialized += (_, _) => Trace("webview-initialized");
        webView.UrlLoading += (_, e) => Trace($"webview-url-loading {e.Url}");
        Loaded += (_, _) => Trace("page-loaded");

        webView.RootComponents.Add(new RootComponent
        {
            Selector = "#app",
            ComponentType = typeof(HybridSmoke)
        });
        Content = webView;
        Trace("page-constructed");
    }

    private static void Trace(string marker)
    {
        Console.WriteLine($"HYBRID-SMOKE {marker}");
        Console.Out.Flush();
    }
}
