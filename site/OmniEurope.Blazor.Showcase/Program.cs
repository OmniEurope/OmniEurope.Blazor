using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Showcase;
using OmniEurope.Blazor.Showcase.Localization;
using OmniEurope.Blazor.Showcase.Theming;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddOmniEuropeBlazor();
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
