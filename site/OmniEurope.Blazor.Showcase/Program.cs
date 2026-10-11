using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Showcase;
using OmniEurope.Blazor.Showcase.Localization;
using OmniEurope.Blazor.Showcase.Preferences;
using OmniEurope.Blazor.Showcase.Theming;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddOmniEuropeBlazor();

// DEMONSTRATION ONLY, opt-in: with ?store=demo in the address the preferences also go to a stand-in
// database kept in the tab's sessionStorage (Preferences/DemoPreferenceStore.cs), so a probe can prove
// the store path of OmniPreferences in a real browser. Without the flag nothing is registered and the
// preferences stay in the browser alone, as for any visitor. The address is read before the host is
// built, through the in-process runtime the WebAssembly builder registers as an instance.
if (builder.Services.FirstOrDefault(service => service.ServiceType == typeof(IJSRuntime))?.ImplementationInstance is IJSInProcessRuntime bootRuntime
    && DemoPreferenceStore.Requested(bootRuntime) is { } demoSyncInterval)
{
    builder.Services.AddOmniEuropePreferences<DemoPreferenceStore>(options => options.SyncInterval = demoSyncInterval);
}

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<ThemeTokenReader>();
builder.Services.AddScoped<ThemeState>();
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var host = builder.Build();

// The showcase runs in the language the visitor chose in its header (saved in the browser), French
// when nothing is saved, whatever the browser language: without an explicit culture, WebAssembly
// takes the browser's, or the invariant one, and a {0:C0} amount shows "¤" instead of "€". The
// culture must be set before RunAsync, which loads the resources of the current culture, and a page
// keeps it for its lifetime, so the selector saves the choice and reloads. The full ICU data and every
// satellite assembly are shipped (BlazorWebAssemblyLoadAllGlobalizationData in the project).
// A missing or failing culture script must not keep the page from starting: it runs in French.
var js = host.Services.GetRequiredService<IJSRuntime>();
var language = ShowcaseLanguages.Default;
try
{
    language = ShowcaseLanguages.Resolve(await js.InvokeAsync<string?>("omniShowcaseCulture.load", ShowcaseLanguages.StorageKey));
    await js.InvokeVoidAsync("omniShowcaseCulture.mark", language.Code);
}
catch (JSException)
{
}

ShowcaseLanguages.Apply(language);

await host.RunAsync();
