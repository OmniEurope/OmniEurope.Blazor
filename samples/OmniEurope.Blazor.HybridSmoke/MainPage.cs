using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.HybridSmoke.Resources;

namespace OmniEurope.Blazor.HybridSmoke;

public sealed class MainPage : ContentPage
{
    // The smoke probe asks WebView2 for a debugging port through this variable. Tracing the value
    // the process actually received is the only way to tell a variable that never arrived from a
    // WebView2 that ignored it: setting it in the parent proves nothing about the child.
    // Applying the arguments here through BlazorWebViewInitializing was tried and does not work:
    // the event hands over a null EnvironmentOptions, and the object assigned to it is discarded.
    private const string BrowserArgumentsVariable = "WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS";

    public MainPage(IStringLocalizer<HybridSmokeStrings> text)
    {
        Title = text["WindowTitle"];
        var webView = new BlazorWebView { HostPage = "wwwroot/index.html" };

        webView.BlazorWebViewInitializing += (_, e) =>
        {
            var arguments = Environment.GetEnvironmentVariable(BrowserArgumentsVariable);
            Trace($"webview-initializing arguments=\"{arguments}\"");
        };
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
