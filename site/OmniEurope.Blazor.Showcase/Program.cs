using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Showcase;
using OmniEurope.Blazor.Showcase.Localization;
using OmniEurope.Blazor.Showcase.Preferences;
using OmniEurope.Blazor.Showcase.Theming;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddOmniEuropeBlazor();
await ShowcaseApiConfiguration.AddLocalOverrideAsync(builder.Configuration, new Uri(builder.HostEnvironment.BaseAddress));

// DEMONSTRATION ONLY, opt-in: with ?store=demo in the address the preferences also go to a stand-in
// database kept in the tab's sessionStorage (Preferences/DemoPreferenceStore.cs), so a probe of the static
// published site can prove the store path of OmniPreferences in a real browser, another device included,
// without an API. It wins over the API store below. The address is read before the host is built,
// through the in-process runtime the WebAssembly builder registers as an instance.
if (builder.Services.FirstOrDefault(service => service.ServiceType == typeof(IJSRuntime))?.ImplementationInstance is IJSInProcessRuntime bootRuntime
    && DemoPreferenceStore.Requested(bootRuntime) is { } demoSyncInterval)
{
    builder.Services.AddOmniEuropePreferences<DemoPreferenceStore>(options => options.SyncInterval = demoSyncInterval);
}
// The deployed showcase: the preferences of the anonymous visitor are kept by the showcase API. Without
// an API address (a static copy of the site) nothing is registered and they stay in the browser alone.
else if (ShowcaseApiConfiguration.BaseAddress(builder.Configuration) is { } apiBaseAddress)
{
    builder.Services.AddSingleton(new ShowcaseApiHttpClient(new HttpClient { BaseAddress = apiBaseAddress, Timeout = TimeSpan.FromSeconds(10) }));
    builder.Services.AddSingleton<ShowcaseVisitor>();
    builder.Services.AddOmniEuropePreferences<ShowcaseApiPreferenceStore>();
}

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<ThemeTokenReader>();
builder.Services.AddScoped<ThemeState>();
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var host = builder.Build();

// The showcase runs in the language the visitor chose in its header (saved in the browser), English
// when nothing is saved, whatever the browser language: without an explicit culture, WebAssembly
// takes the browser's, or the invariant one, and a {0:C0} amount shows "¤" instead of "€". The
// culture must be set before RunAsync, which loads the resources of the current culture, and a page
// keeps it for its lifetime, so the selector saves the choice and reloads. English and French both
// format with the EFIGS ICU shard the project pins (BlazorIcuDataFileName).
// A missing or failing culture script must not keep the page from starting: it runs in English.
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
