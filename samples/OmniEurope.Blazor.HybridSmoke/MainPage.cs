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
        webView.RootComponents.Add(new RootComponent
        {
            Selector = "#app",
            ComponentType = typeof(HybridSmoke)
        });
        Content = webView;
    }
}
