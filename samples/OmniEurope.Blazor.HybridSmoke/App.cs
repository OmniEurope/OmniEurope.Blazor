using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.HybridSmoke.Resources;

namespace OmniEurope.Blazor.HybridSmoke;

public sealed class App(IStringLocalizer<HybridSmokeStrings> text) : Application
{
    protected override Window CreateWindow(IActivationState? activationState) => new(new MainPage(text));
}
